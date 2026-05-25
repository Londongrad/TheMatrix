using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class HouseholdCommutePressureProfileBuilderTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        private static readonly HouseholdId TestHouseholdId =
            HouseholdId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        private static readonly ResidentialBuildingId TestResidentialBuildingId = ResidentialBuildingId.From(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 6);

        [Fact]
        public async Task BuildAsync_WhenHouseholdHasNoResidentialBuilding_ReturnsNullAndDoesNotCallRouting()
        {
            CityAnchorId workplaceAnchorId = CreateAnchorId(1);
            Person resident = CreateEmployedResident(
                householdId: TestHouseholdId,
                workplaceAnchorId: workplaceAnchorId);
            var routingService = new RecordingCommuteRoutingService();

            CityHouseholdCommutePressureProfile? profile = await HouseholdCommutePressureProfileBuilder.BuildAsync(
                cityId: TestCityId,
                householdId: TestHouseholdId,
                householdResidents: [resident],
                residentialBuildingByHouseholdId: new Dictionary<HouseholdId, ResidentialBuildingId?>(),
                commuteRoutingService: routingService,
                cancellationToken: CancellationToken.None);

            Assert.Null(profile);
            Assert.Empty(routingService.PreloadRequests);
            Assert.Empty(routingService.ResolvedDestinationAnchorIds);
        }

        [Fact]
        public async Task BuildAsync_WhenNoAliveResidentsHaveDestination_ReturnsNull()
        {
            Person unemployed = CreatePerson(
                personId: Guid.NewGuid(),
                householdId: TestHouseholdId.Value);
            var routingService = new RecordingCommuteRoutingService();

            CityHouseholdCommutePressureProfile? profile = await HouseholdCommutePressureProfileBuilder.BuildAsync(
                cityId: TestCityId,
                householdId: TestHouseholdId,
                householdResidents: [unemployed],
                residentialBuildingByHouseholdId: CreateResidentialBuildingMap(),
                commuteRoutingService: routingService,
                cancellationToken: CancellationToken.None);

            Assert.Null(profile);
            Assert.Empty(routingService.PreloadRequests);
            Assert.Empty(routingService.ResolvedDestinationAnchorIds);
        }

        [Fact]
        public async Task BuildAsync_WhenResidentsHaveCommuteDestinations_PreloadsAndResolvesRoutes()
        {
            CityAnchorId workplaceAnchorId = CreateAnchorId(2);
            CityAnchorId institutionAnchorId = CreateAnchorId(3);
            Person employee = CreateEmployedResident(
                householdId: TestHouseholdId,
                workplaceAnchorId: workplaceAnchorId);
            Person student = CreateStudentResident(
                householdId: TestHouseholdId,
                institutionAnchorId: institutionAnchorId);
            Person unemployed = CreatePerson(
                personId: Guid.NewGuid(),
                householdId: TestHouseholdId.Value);
            var routingService = new RecordingCommuteRoutingService();
            routingService.SetContext(
                destinationAnchorId: workplaceAnchorId,
                context: CreateCommute(
                    isAccessible: true,
                    accessibilityIndex: 0.8m,
                    estimatedTravelTimeMinutes: 45m));
            routingService.SetContext(
                destinationAnchorId: institutionAnchorId,
                context: CreateCommute(
                    isAccessible: true,
                    accessibilityIndex: 1m,
                    estimatedTravelTimeMinutes: 15m));

            CityHouseholdCommutePressureProfile? profile = await HouseholdCommutePressureProfileBuilder.BuildAsync(
                cityId: TestCityId,
                householdId: TestHouseholdId,
                householdResidents:
                [
                    employee,
                    student,
                    unemployed
                ],
                residentialBuildingByHouseholdId: CreateResidentialBuildingMap(),
                commuteRoutingService: routingService,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(profile);
            Assert.Equal(
                expected: 2,
                actual: routingService.PreloadRequests.Count);
            Assert.All(
                collection: routingService.PreloadRequests,
                action: request =>
                {
                    Assert.Equal(
                        expected: TestResidentialBuildingId,
                        actual: request.ResidentialBuildingId);
                    Assert.Equal(
                        expected: CityPopulationCommuteRoutingProfiles.Pedestrian,
                        actual: request.Profile);
                });
            Assert.Equal(
                expectedSpan:
                [
                    workplaceAnchorId,
                    institutionAnchorId
                ],
                actualArray: routingService.PreloadRequests.Select(request => request.DestinationAnchorId)
                   .ToArray());
            Assert.Equal(
                expected:
                [
                    workplaceAnchorId,
                    institutionAnchorId
                ],
                actual: routingService.ResolvedDestinationAnchorIds.ToArray());
            Assert.Equal(
                expected: 2,
                actual: profile.RoutedResidentCount);
            Assert.Equal(
                expected: 0,
                actual: profile.BlockedRouteCount);
            Assert.Equal(
                expected: 0.1000m,
                actual: profile.AccessibilityDeficitIndex);
            Assert.Equal(
                expected: 0.3333m,
                actual: profile.TravelFatigueIndex);
        }

        [Fact]
        public async Task BuildAsync_WhenRouteIsInaccessibleWithoutTravelTime_UsesFullTravelFatigue()
        {
            CityAnchorId workplaceAnchorId = CreateAnchorId(4);
            Person employee = CreateEmployedResident(
                householdId: TestHouseholdId,
                workplaceAnchorId: workplaceAnchorId);
            var routingService = new RecordingCommuteRoutingService();
            routingService.SetContext(
                destinationAnchorId: workplaceAnchorId,
                context: CreateCommute(
                    isAccessible: false,
                    accessibilityIndex: 0.25m,
                    estimatedTravelTimeMinutes: null));

            CityHouseholdCommutePressureProfile? profile = await HouseholdCommutePressureProfileBuilder.BuildAsync(
                cityId: TestCityId,
                householdId: TestHouseholdId,
                householdResidents: [employee],
                residentialBuildingByHouseholdId: CreateResidentialBuildingMap(),
                commuteRoutingService: routingService,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(profile);
            Assert.Equal(
                expected: 1,
                actual: profile.RoutedResidentCount);
            Assert.Equal(
                expected: 1,
                actual: profile.BlockedRouteCount);
            Assert.Equal(
                expected: 0.7500m,
                actual: profile.AccessibilityDeficitIndex);
            Assert.Equal(
                expected: 1.0000m,
                actual: profile.TravelFatigueIndex);
        }

        [Fact]
        public async Task BuildAsync_WhenTravelTimeExceedsNinetyMinutes_ClampsTravelFatigue()
        {
            CityAnchorId workplaceAnchorId = CreateAnchorId(5);
            Person employee = CreateEmployedResident(
                householdId: TestHouseholdId,
                workplaceAnchorId: workplaceAnchorId);
            var routingService = new RecordingCommuteRoutingService();
            routingService.SetContext(
                destinationAnchorId: workplaceAnchorId,
                context: CreateCommute(
                    isAccessible: true,
                    accessibilityIndex: 1m,
                    estimatedTravelTimeMinutes: 180m));

            CityHouseholdCommutePressureProfile? profile = await HouseholdCommutePressureProfileBuilder.BuildAsync(
                cityId: TestCityId,
                householdId: TestHouseholdId,
                householdResidents: [employee],
                residentialBuildingByHouseholdId: CreateResidentialBuildingMap(),
                commuteRoutingService: routingService,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(profile);
            Assert.Equal(
                expected: 1,
                actual: profile.RoutedResidentCount);
            Assert.Equal(
                expected: 0,
                actual: profile.BlockedRouteCount);
            Assert.Equal(
                expected: 0.0000m,
                actual: profile.AccessibilityDeficitIndex);
            Assert.Equal(
                expected: 1.0000m,
                actual: profile.TravelFatigueIndex);
        }

        private static IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> CreateResidentialBuildingMap()
        {
            return new Dictionary<HouseholdId, ResidentialBuildingId?>
            {
                [TestHouseholdId] = TestResidentialBuildingId
            };
        }

        private static Person CreateEmployedResident(
            HouseholdId householdId,
            CityAnchorId workplaceAnchorId)
        {
            return CreatePerson(
                personId: Guid.NewGuid(),
                householdId: householdId.Value,
                employmentStatus: EmploymentStatus.Employed,
                job: new Job(
                    workplaceId: WorkplaceId.From(Guid.NewGuid()),
                    title: "Engineer",
                    workplaceAnchorId: workplaceAnchorId));
        }

        private static Person CreateStudentResident(
            HouseholdId householdId,
            CityAnchorId institutionAnchorId)
        {
            Person resident = CreatePerson(
                personId: Guid.NewGuid(),
                householdId: householdId.Value);

            resident.StartStudying(
                currentDate: CurrentDate,
                institutionId: EducationInstitutionId.From(Guid.NewGuid()),
                institutionAnchorId: institutionAnchorId);

            return resident;
        }

        private static CityPopulationCommuteContext CreateCommute(
            bool isAccessible,
            decimal accessibilityIndex,
            decimal? estimatedTravelTimeMinutes)
        {
            return new CityPopulationCommuteContext(
                HasRouteData: true,
                IsAccessible: isAccessible,
                AccessibilityIndex: accessibilityIndex,
                PassabilityIndex: isAccessible
                    ? 1m
                    : 0m,
                EstimatedTravelTimeMinutes: estimatedTravelTimeMinutes);
        }

        private static CityAnchorId CreateAnchorId(int index)
        {
            return CityAnchorId.From(Guid.Parse($"dddddddd-dddd-dddd-dddd-{index:000000000000}"));
        }

        private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
        {
            private readonly Dictionary<CityAnchorId, CityPopulationCommuteContext> _contexts = [];

            public List<CityPopulationCommuteRouteRequest> PreloadRequests { get; } = [];
            public List<CityAnchorId?> ResolvedDestinationAnchorIds { get; } = [];

            public Task PreloadAnchorCommutesAsync(
                Guid cityId,
                IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
                CancellationToken cancellationToken)
            {
                PreloadRequests.AddRange(requests);
                return Task.CompletedTask;
            }

            public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? destinationAnchorId,
                CancellationToken cancellationToken)
            {
                ResolvedDestinationAnchorIds.Add(destinationAnchorId);

                return Task.FromResult(
                    destinationAnchorId.HasValue &&
                    _contexts.TryGetValue(
                        key: destinationAnchorId.Value,
                        value: out CityPopulationCommuteContext? context)
                        ? context
                        : CityPopulationCommuteContext.Neutral);
            }

            public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? healthcareAnchorId,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public void SetContext(
                CityAnchorId destinationAnchorId,
                CityPopulationCommuteContext context)
            {
                _contexts[destinationAnchorId] = context;
            }
        }
    }
}
