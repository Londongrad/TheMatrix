using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using System.Text.Json;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationSummaryProjectionServiceTests
    {
        [Theory]
        [InlineData(true, false, false, true, false)]
        [InlineData(false, false, false, true, false)]
        [InlineData(true, true, false, true, false)]
        [InlineData(true, true, false, false, false)]
        [InlineData(true, true, false, true, true)]
        [InlineData(true, false, true, true, false)]
        [InlineData(false, false, true, true, false)]
        public async Task UpdateAsync_UsesExternalAttendanceAndCollectsFactsOnlyWhenRequested(
            bool isEnrolled, bool hasAttendance, bool collectObservation, bool includeCommuteMetrics, bool staleDay)
        {
            await using var database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            Guid cityValue = Guid.NewGuid();
            var cityId = CityId.From(cityValue);
            Guid householdValue = Guid.NewGuid();
            Guid institutionAnchorId = Guid.NewGuid();
            Household household = CreateHousehold(householdValue, size: 1);
            Person resident = CreatePerson(householdId: householdValue);
            ClassicCityHouseholdPlacement placement = ClassicCityHouseholdPlacement.CreateHoused(
                household.Id,
                cityId,
                DistrictId.From(Guid.NewGuid()),
                ResidentialBuildingId.From(Guid.NewGuid()));
            dbContext.Households.Add(household);
            dbContext.Persons.Add(resident);
            dbContext.ClassicCityHouseholdPlacements.Add(placement);
            await dbContext.SaveChangesAsync();

            var educationRepository = new EducationParticipationProjectionRepository(dbContext);
            await educationRepository.UpsertNewerAsync(
            [
                new EducationParticipationProjection(
                    SimulationHostId: cityValue,
                    ResidentId: resident.Id.Value,
                    ParticipationRevision: 1,
                    ResidentLifecycleRevision: resident.LifecycleRevision,
                    IsEnrolled: isEnrolled,
                    ActiveStage: isEnrolled ? "higher" : null,
                    InstitutionId: isEnrolled ? Guid.NewGuid() : null,
                    InstitutionAnchorId: isEnrolled ? institutionAnchorId : null,
                    EnrolledOn: isEnrolled ? new DateOnly(2048, 5, 1) : null,
                    CompletedStage: "upper-secondary",
                    CompletedStageOn: new DateOnly(2048, 4, 30),
                    SnapshotDate: new DateOnly(2048, 5, 2),
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    UpdatedAtUtc: DateTimeOffset.UtcNow)
            ]);
            await dbContext.SaveChangesAsync();
            var observedAt = new DateTimeOffset(2048, 5, staleDay ? 1 : 2, 9, 0, 0, TimeSpan.Zero);
            if (hasAttendance)
            {
                await new EducationAttendanceProjectionWriter(dbContext).ApplyAsync(cityValue, 5, observedAt,
                    [new(resident.Id.Value, resident.LifecycleRevision, 1, 0.73m, 0.84m)], default);
                await dbContext.SaveChangesAsync();
            }
            var routeResolutionClient = new NeutralRouteResolutionClient();
            var service = new CityPopulationSummaryProjectionService(
                dbContext,
                new CityPopulationDistrictImpactPolicy(),
                new CityPopulationParticipationPolicy(),
                TimeProvider.System,
                new CityPopulationCommuteRoutingService(routeResolutionClient),
                educationRepository,
                NullLogger<CityPopulationSummaryProjectionService>.Instance);

            await service.UpdateAsync(
                cityId,
                new DateOnly(2048, 5, 2),
                [resident],
                [placement],
                includeCommuteMetrics: includeCommuteMetrics,
                activityObservation: collectObservation ? new(6, observedAt) : null);
            await dbContext.SaveChangesAsync();

            CityPopulationSummaryProjection summary = await dbContext.CityPopulationSummaryProjections
               .AsNoTracking()
               .SingleAsync(projection => projection.CityId == cityId);
            Assert.Equal(isEnrolled ? 1 : 0, summary.StudentCount);
            Assert.Equal(1, summary.UnemployedCount);
            Assert.Equal(hasAttendance && !staleDay ? 0.73m : (decimal?)null, summary.StudentAttendanceIndex);
            Assert.Equal(hasAttendance && !staleDay ? 0.84m : (decimal?)null, summary.StudentCommuteAccessibilityIndex);
            Assert.Empty(routeResolutionClient.RequestedAnchorIds);
            if (collectObservation && isEnrolled)
            {
                Assert.Equal(1, routeResolutionClient.BatchCalls);
                var outbox = Assert.Single(await dbContext.OutboxMessages.ToListAsync());
                Assert.Equal(ClassicCityOutboxEventTypes.ResidentActivityConditionsBatchV1, outbox.Type);
                var message = JsonSerializer.Deserialize<ClassicCityResidentActivityConditionsBatchV1>(outbox.PayloadJson,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                Assert.Equal(6, message.SourceTickId);
                var facts = Assert.Single(message.Residents);
                Assert.Equal(resident.Energy.Value, facts.Energy);
                Assert.Equal(resident.LifecycleRevision, facts.ResidentLifecycleRevision);
                Assert.Equal(placement.DistrictId!.Value.Value, Assert.Single(message.Areas).DistrictId);
            }
            else
            {
                Assert.Equal(0, routeResolutionClient.BatchCalls);
                Assert.Empty(await dbContext.OutboxMessages.ToListAsync());
            }
        }

        [Fact]
        public async Task CollectAsync_BoundsBatchesAndDeduplicatesDistrictsAndRoutes()
        {
            Guid householdId = Guid.NewGuid();
            var cityId = CityId.From(Guid.NewGuid());
            var household = CreateHousehold(householdId);
            var placement = ClassicCityHouseholdPlacement.CreateHoused(household.Id, cityId,
                DistrictId.From(Guid.NewGuid()), ResidentialBuildingId.From(Guid.NewGuid()));
            var now = new DateTimeOffset(2048, 5, 2, 9, 0, 0, TimeSpan.Zero);
            var persons = Enumerable.Range(0, 1001).Select(_ => CreatePerson(personId: Guid.NewGuid(), householdId: householdId)).ToArray();
            var anchor = Guid.NewGuid();
            var participations = persons.ToDictionary(person => person.Id.Value, person =>
                new EducationParticipationProjection(cityId.Value, person.Id.Value, 1, person.LifecycleRevision, true,
                    "higher", Guid.NewGuid(), anchor, new DateOnly(2048, 1, 1), null, null, new DateOnly(2048, 5, 2), now, now));
            var client = new NeutralRouteResolutionClient();
            var batches = await ClassicCityActivityConditionsCollector.CollectAsync(cityId, new(5, now), now, persons,
                new EducationParticipationProjectionIndex(cityId.Value, participations), [placement], null, null,
                new CityPopulationDistrictImpactPolicy(), new CityPopulationCommuteRoutingService(client), default);
            Assert.Equal(2, batches.Count);
            Assert.Equal(1000, batches[0].Residents.Count);
            Assert.Single(batches[1].Residents);
            Assert.All(batches, batch =>
            {
                Assert.Equal(2, batch.TotalBatches);
                Assert.Single(batch.Areas);
                Assert.All(batch.Residents, resident => Assert.Equal(0, resident.AreaIndex));
            });
            Assert.Equal(1, batches[0].BatchNumber);
            Assert.Equal(2, batches[1].BatchNumber);
            Assert.Equal(1, client.BatchCalls);
            Assert.Equal(1, client.BatchRouteCount);
            Assert.Empty(client.RequestedAnchorIds);
        }

        private sealed class NeutralRouteResolutionClient : ICityRouteResolutionClient
        {
            public List<Guid> RequestedAnchorIds { get; } = [];
            public int BatchCalls { get; private set; }
            public int BatchRouteCount { get; private set; }

            public Task<CityPopulationCommuteContext?> ResolveResidentialToAnchorAsync(
                Guid cityId,
                ResidentialBuildingId residentialBuildingId,
                CityAnchorId cityAnchorId,
                string profile,
                CancellationToken cancellationToken)
            {
                RequestedAnchorIds.Add(cityAnchorId.Value);
                return Task.FromResult<CityPopulationCommuteContext?>(
                    CityPopulationCommuteContext.Neutral);
            }

            public Task<IReadOnlyDictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?>>
                ResolveResidentialToAnchorsAsync(
                    Guid cityId,
                    IReadOnlyCollection<CityRouteResolutionBatchRequestItem> requests,
                    CancellationToken cancellationToken)
            {
                BatchCalls++;
                BatchRouteCount += requests.Count;
                IReadOnlyDictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?> result =
                    requests.ToDictionary(
                        request => request,
                        _ => (CityPopulationCommuteContext?)CityPopulationCommuteContext.Neutral);
                return Task.FromResult(result);
            }
        }
    }
}
