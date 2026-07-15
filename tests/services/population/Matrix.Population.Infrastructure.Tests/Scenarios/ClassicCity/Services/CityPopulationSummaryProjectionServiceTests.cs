using Matrix.Population.Application.Integration.Education;
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
        [Fact]
        public async Task UpdateAsync_CountsProjectedLearnerIndependentlyFromEmployment()
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
                    IsEnrolled: true,
                    ActiveStage: "higher",
                    InstitutionId: Guid.NewGuid(),
                    InstitutionAnchorId: institutionAnchorId,
                    EnrolledOn: new DateOnly(2048, 5, 1),
                    CompletedStage: "upper-secondary",
                    CompletedStageOn: new DateOnly(2048, 4, 30),
                    SnapshotDate: new DateOnly(2048, 5, 2),
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    UpdatedAtUtc: DateTimeOffset.UtcNow)
            ]);
            await dbContext.SaveChangesAsync();
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
                includeCommuteMetrics: true);

            CityPopulationSummaryProjection summary = await dbContext.CityPopulationSummaryProjections
               .AsNoTracking()
               .SingleAsync(projection => projection.CityId == cityId);
            Assert.Equal(1, summary.StudentCount);
            Assert.Equal(1, summary.UnemployedCount);
            Assert.NotNull(summary.StudentAttendanceIndex);
            Assert.Equal(
                institutionAnchorId,
                Assert.Single(routeResolutionClient.RequestedAnchorIds));
        }

        private sealed class NeutralRouteResolutionClient : ICityRouteResolutionClient
        {
            public List<Guid> RequestedAnchorIds { get; } = [];

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
                IReadOnlyDictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?> result =
                    requests.ToDictionary(
                        request => request,
                        _ => (CityPopulationCommuteContext?)CityPopulationCommuteContext.Neutral);
                return Task.FromResult(result);
            }
        }
    }
}
