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
                    day: 4,
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

        [Fact]
        public async Task SyncAsync_PreservesDispatchLimitAndSkipsResidentsAlreadyTravelling()
        {
            Guid householdId = Guid.NewGuid();
            var residents = Enumerable.Range(0, 64).Select(_ => CreatePerson(personId: Guid.NewGuid(),
                householdId: householdId, employmentStatus: EmploymentStatus.Unemployed)).ToArray();
            var home = ResidentialBuildingId.From(Guid.NewGuid());
            var placement = ClassicCityHouseholdPlacement.CreateHoused(residents[0].HouseholdId,
                Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.CityId.From(CityId), DistrictId.From(Guid.NewGuid()), home);
            var routine = PersonRoutineProfile.Structured(TimeSpan.FromHours(12), TimeSpan.FromHours(16),
                PersonStructuredActivityLoad.Moderate, PersonRoutineDays.Saturday);
            var activities = residents.ToDictionary(resident => resident.Id, resident => new ResidentExternalActivityProfile(
                resident.LifecycleRevision, routine, InstitutionAnchorId, ExternalCommutePurpose, ResidentWorkforceQualificationTier.None));
            var now = new DateTimeOffset(2048, 5, 2, 12, 0, 0, TimeSpan.Zero);
            var travelling = residents.Take(32).Select(resident => resident.Id.Value).ToHashSet();
            var trips = new FakeCityPopulationActiveTripClient
            {
                ActiveTripsByCity = travelling.Select(id => new CityPopulationActiveTripSnapshot(id, "existing", ExternalCommutePurpose,
                    "InProgress", 0.5m, now.AddMinutes(-10), now.AddMinutes(10), "home", home.Value, "activity", InstitutionAnchorId)).ToArray()
            };
            var routes = new FakeCityPopulationCommuteRoutingService { AnchorContext = new(true, true, 1m, 1m, 20m) };
            await new CityPopulationCommuteTripSyncService(trips, routes).SyncAsync(CityId, 42, now,
                residents, [placement], activities, default);
            Assert.Equal(12, trips.DispatchRequests.Count);
            Assert.Equal(12, trips.DispatchRequests.Select(request => request.TravellerEntityId).Distinct().Count());
            Assert.All(trips.DispatchRequests, request => Assert.DoesNotContain(request.TravellerEntityId!.Value, travelling));
        }

        [Theory]
        [InlineData(2, 9, 180, 600, 840, "outbound")]
        [InlineData(2, 12, 180, 600, 840, "return")]
        [InlineData(2, 17, 180, 600, 840, "none")]
        [InlineData(2, 11, 0, 600, 840, "outbound")]
        [InlineData(3, 9, 180, 600, 840, "none")]
        [InlineData(4, 9, 180, 600, 840, "none")]
        [InlineData(1, 22, 0, 0, 60, "outbound")]
        [InlineData(1, 23, 180, 0, 60, "return")]
        [InlineData(3, 1, 0, 1380, 1440, "return")]
        public async Task SyncAsync_UsesLocalScheduleForOutboundAndReturnWindows(
            int day, int hourUtc, int utcOffset, int startMinute, int endMinute, string expected)
        {
            var resident = CreatePerson(employmentStatus: EmploymentStatus.Unemployed);
            var home = ResidentialBuildingId.From(Guid.NewGuid());
            var placement = ClassicCityHouseholdPlacement.CreateHoused(resident.HouseholdId,
                Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects.CityId.From(CityId), DistrictId.From(Guid.NewGuid()), home);
            var activity = new ResidentExternalActivityProfile(resident.LifecycleRevision,
                PersonRoutineProfile.Structured(TimeSpan.FromMinutes(startMinute), TimeSpan.FromMinutes(endMinute),
                    PersonStructuredActivityLoad.Moderate, PersonRoutineDays.Saturday),
                InstitutionAnchorId, ExternalCommutePurpose, ResidentWorkforceQualificationTier.None);
            var trips = new FakeCityPopulationActiveTripClient();
            var routes = new FakeCityPopulationCommuteRoutingService
            {
                AnchorContext = new(true, true, 1m, 1m, 20m)
            };
            await new CityPopulationCommuteTripSyncService(trips, routes).SyncAsync(CityId, 42,
                new DateTimeOffset(2048, 5, day, hourUtc, 0, 0, TimeSpan.Zero), [resident], [placement],
                new Dictionary<PersonId, ResidentExternalActivityProfile> { [resident.Id] = activity }, default, utcOffset);
            if (expected == "none")
            {
                Assert.Null(trips.RequestedCityId);
                Assert.Null(trips.RequestedDispatch);
            }
            else
            {
                Assert.NotNull(trips.RequestedDispatch);
                Assert.Equal(expected == "outbound" ? InstitutionAnchorId : home.Value, trips.RequestedDispatch.ToId);
                Assert.Equal(expected == "outbound" ? home.Value : InstitutionAnchorId, trips.RequestedDispatch.FromId);
                Assert.Equal(ExternalCommutePurpose, trips.RequestedDispatch.Purpose);
            }
        }
    }
}
