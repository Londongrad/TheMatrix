using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.World;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
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
        private const string ExternalCommutePurpose = "ShelterTrainingCommute";

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task SyncAsync_UsesOnlyLivingCurrentLifecycleSnapshot(
            bool dieAfterSnapshot,
            bool resurrectAfterDeath)
        {
            Person enrolledResident = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                employmentStatus: EmploymentStatus.Unemployed);
            Person residentWithoutParticipation = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: enrolledResident.HouseholdId.Value,
                employmentStatus: EmploymentStatus.Unemployed);
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
                commuteRoutingService: commuteRoutingService);
            var activities = new Dictionary<PersonId, ResidentExternalActivityProfile>
            {
                [enrolledResident.Id] = new ResidentExternalActivityProfile(
                    ResidentLifecycleRevision: enrolledResident.LifecycleRevision,
                    Routine: PersonRoutineProfile.Structured(
                        activityStart: TimeSpan.FromHours(8),
                        activityEnd: TimeSpan.FromHours(15),
                        activityLoad: PersonStructuredActivityLoad.Moderate),
                    DestinationAnchorId: InstitutionAnchorId,
                    CommutePurpose: ExternalCommutePurpose,
                    WorkforceQualification: ResidentWorkforceQualificationTier.General),
                [residentWithoutParticipation.Id] = ResidentExternalActivityProfile.None
            };
            if (dieAfterSnapshot)
                enrolledResident.Die(new DateOnly(2048, 5, 3));
            if (resurrectAfterDeath)
                enrolledResident.Resurrect();

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
                externalActivitiesByResidentId: activities,
                cancellationToken: CancellationToken.None);
            if (dieAfterSnapshot)
            {
                Assert.Null(activeTripClient.RequestedDispatch);
                return;
            }

            CityPopulationTripDispatchRequest dispatch = Assert.IsType<CityPopulationTripDispatchRequest>(
                activeTripClient.RequestedDispatch);
            Assert.Equal(
                expected: enrolledResident.Id.Value,
                actual: dispatch.TravellerEntityId);
            Assert.Equal(
                expected: ExternalCommutePurpose,
                actual: dispatch.Purpose);
            Assert.Equal(
                expected: InstitutionAnchorId,
                actual: dispatch.ToId);
        }
    }
}
