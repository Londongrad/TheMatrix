using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.Services.World
{
    public sealed class CityPopulationCommuteTripSyncServiceTests
    {
        private static readonly Guid CityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid InstitutionAnchorId =
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public async Task SyncAsync_UsesCurrentEducationProjectionForAllResidentsInOneBatch()
        {
            Person enrolledResident = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                employmentStatus: EmploymentStatus.Unemployed);
            Person residentWithoutParticipation = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: enrolledResident.HouseholdId.Value,
                employmentStatus: EmploymentStatus.Unemployed);
            var projectionRepository = new FakeEducationParticipationProjectionRepository();
            await projectionRepository.UpsertNewerAsync(
                projections:
                [
                    CreateProjection(enrolledResident)
                ]);
            var activeTripClient = new FakeCityPopulationActiveTripClient();
            var commuteRoutingService = new FakeCityPopulationCommuteRoutingService
            {
                AnchorContext = new CityPopulationCommuteContext(
                    HasRouteData: true,
                    IsAccessible: true,
                    AccessibilityIndex: 0.85m,
                    PassabilityIndex: 0.90m,
                    EstimatedTravelTimeMinutes: 20m)
            };
            var service = new CityPopulationCommuteTripSyncService(
                activeTripClient: activeTripClient,
                commuteRoutingService: commuteRoutingService,
                educationParticipationProjectionRepository: projectionRepository);

            await service.SyncAsync(
                cityId: CityId,
                tickId: 42,
                currentSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                residents:
                [
                    enrolledResident,
                    residentWithoutParticipation
                ],
                householdPlacements:
                [
                    ClassicCityHouseholdPlacement.CreateHoused(
                        householdId: enrolledResident.HouseholdId,
                        cityId: Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.CityId.From(CityId),
                        districtId: DistrictId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
                        residentialBuildingId: ResidentialBuildingId.From(
                            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")))
                ],
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: projectionRepository.GetByResidentIdsCallCount);
            Assert.Equal(
                expected: CityId,
                actual: projectionRepository.RequestedSimulationHostId);
            Assert.Equal(
                expected:
                [
                    enrolledResident.Id.Value,
                    residentWithoutParticipation.Id.Value
                ],
                actual: projectionRepository.RequestedResidentIds);
            CityPopulationTripDispatchRequest dispatch = Assert.IsType<CityPopulationTripDispatchRequest>(
                activeTripClient.RequestedDispatch);
            Assert.Equal(
                expected: enrolledResident.Id.Value,
                actual: dispatch.TravellerEntityId);
            Assert.Equal(
                expected: "EducationCommute",
                actual: dispatch.Purpose);
            Assert.Equal(
                expected: InstitutionAnchorId,
                actual: dispatch.ToId);
        }

        private static EducationParticipationProjection CreateProjection(Person resident)
        {
            return new EducationParticipationProjection(
                SimulationHostId: CityId,
                ResidentId: resident.Id.Value,
                ParticipationRevision: 3,
                ResidentLifecycleRevision: resident.LifecycleRevision,
                IsEnrolled: true,
                ActiveStage: "upper-secondary",
                InstitutionId: Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                InstitutionAnchorId: InstitutionAnchorId,
                EnrolledOn: new DateOnly(2048, 1, 10),
                CompletedStage: "lower-secondary",
                CompletedStageOn: new DateOnly(2047, 6, 20),
                SnapshotDate: new DateOnly(2048, 5, 3),
                OccurredAtUtc: UtcNow,
                UpdatedAtUtc: UtcNow);
        }
    }
}
