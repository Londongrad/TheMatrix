using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class ResidentHouseholdPressureProgressionStepTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        private static readonly HouseholdId TestHouseholdId =
            HouseholdId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        private static readonly ResidentialBuildingId TestResidentialBuildingId = ResidentialBuildingId.From(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

        private static readonly DateOnly PreviousDate = new(
            year: 2048,
            month: 5,
            day: 5);

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 6);

        [Fact]
        public async Task ApplyAsync_WhenHouseholdResidentsAreMissing_ReturnsFalse()
        {
            Person resident = CreateResident();
            PressureSnapshot initial = Capture(resident);
            var routingService = new RecordingCommuteRoutingService();

            bool changed = await ApplyAsync(
                resident: resident,
                commuteRoutingService: routingService);

            Assert.False(changed);
            Assert.Equal(
                expected: initial,
                actual: Capture(resident));
            Assert.Empty(routingService.PreloadRequests);
            Assert.Empty(routingService.ResolvedAnchorIds);
        }

        [Fact]
        public async Task ApplyAsync_WhenHouseholdResidentsAreEmpty_ReturnsFalse()
        {
            Person resident = CreateResident();
            PressureSnapshot initial = Capture(resident);
            var routingService = new RecordingCommuteRoutingService();

            bool changed = await ApplyAsync(
                resident: resident,
                residentsByHouseholdId: new Dictionary<HouseholdId, IReadOnlyCollection<Person>>
                {
                    [TestHouseholdId] = []
                },
                commuteRoutingService: routingService);

            Assert.False(changed);
            Assert.Equal(
                expected: initial,
                actual: Capture(resident));
            Assert.Empty(routingService.PreloadRequests);
            Assert.Empty(routingService.ResolvedAnchorIds);
        }

        [Fact]
        public async Task ApplyAsync_WhenCurrentDateDoesNotAdvance_ReturnsFalseAndDoesNotChangeResident()
        {
            Person resident = CreateResident();
            PressureSnapshot initial = Capture(resident);

            bool changed = await ApplyAsync(
                resident: resident,
                residentsByHouseholdId: CreateResidentsMap(resident),
                previousDate: CurrentDate,
                currentDate: CurrentDate);

            Assert.False(changed);
            Assert.Equal(
                expected: initial,
                actual: Capture(resident));
        }

        [Fact]
        public async Task ApplyAsync_WhenHouseholdPressureIsHigh_AppliesPressureEffect()
        {
            Person resident = CreateResident();
            PressureSnapshot initial = Capture(resident);

            bool changed = await ApplyAsync(
                resident: resident,
                residentsByHouseholdId: CreateResidentsMap(resident),
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [TestHouseholdId] = HousingStatus.Homeless
                },
                financialStressByHouseholdId: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                {
                    [TestHouseholdId] = CreateFinancialStressState()
                });

            PressureSnapshot actual = Capture(resident);
            Assert.True(changed);
            Assert.True(actual.Happiness < initial.Happiness);
            Assert.True(actual.Energy < initial.Energy);
            Assert.True(actual.Stress > initial.Stress);
            Assert.True(actual.SocialNeed > initial.SocialNeed);
            Assert.True(resident.IsAlive);
        }

        [Fact]
        public async Task ApplyAsync_WhenHouseholdResidentsHaveCommuteDestinations_BuildsCommuteProfileFromRouting()
        {
            CityAnchorId workplaceAnchorId = CreateAnchorId();
            Person resident = CreateEmployedResident(workplaceAnchorId);
            var routingService = new RecordingCommuteRoutingService();
            routingService.SetAnchorContext(
                anchorId: workplaceAnchorId,
                context: new CityPopulationCommuteContext(
                    HasRouteData: true,
                    IsAccessible: false,
                    AccessibilityIndex: 0.25m,
                    PassabilityIndex: 0m,
                    EstimatedTravelTimeMinutes: null));

            await ApplyAsync(
                resident: resident,
                residentsByHouseholdId: CreateResidentsMap(resident),
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [TestHouseholdId] = HousingStatus.Housed
                },
                residentialBuildingByHouseholdId: new Dictionary<HouseholdId, ResidentialBuildingId?>
                {
                    [TestHouseholdId] = TestResidentialBuildingId
                },
                commuteRoutingService: routingService);

            CityPopulationCommuteRouteRequest request = Assert.Single(routingService.PreloadRequests);
            Assert.Equal(
                expected: TestResidentialBuildingId,
                actual: request.ResidentialBuildingId);
            Assert.Equal(
                expected: workplaceAnchorId,
                actual: request.DestinationAnchorId);
            Assert.Equal(
                expected: CityPopulationCommuteRoutingProfiles.Pedestrian,
                actual: request.Profile);
            Assert.Equal(
                expected: [workplaceAnchorId],
                actual: routingService.ResolvedAnchorIds);
        }

        [Fact]
        public async Task ApplyAsync_WhenResidentIsAlreadyDead_ReturnsFalse()
        {
            Person resident = CreateResident(lifeStatus: LifeStatus.Deceased);
            PressureSnapshot initial = Capture(resident);

            bool changed = await ApplyAsync(
                resident: resident,
                residentsByHouseholdId: CreateResidentsMap(resident),
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [TestHouseholdId] = HousingStatus.Homeless
                },
                financialStressByHouseholdId: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                {
                    [TestHouseholdId] = CreateFinancialStressState()
                });

            Assert.False(changed);
            Assert.Equal(
                expected: initial,
                actual: Capture(resident));
            Assert.False(resident.IsAlive);
        }

        private static Task<bool> ApplyAsync(
            Person resident,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<Person>>? residentsByHouseholdId = null,
            IReadOnlyDictionary<HouseholdId, HousingStatus>? housingByHouseholdId = null,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?>? residentialBuildingByHouseholdId = null,
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>?
                financialStressByHouseholdId = null,
            RecordingCommuteRoutingService? commuteRoutingService = null,
            CityHouseholdPressurePolicy? householdPressurePolicy = null,
            DateOnly? previousDate = null,
            DateOnly? currentDate = null)
        {
            return ResidentHouseholdPressureProgressionStep.ApplyAsync(
                cityId: TestCityId,
                person: resident,
                residentsByHouseholdId: residentsByHouseholdId ??
                                        new Dictionary<HouseholdId, IReadOnlyCollection<Person>>(),
                previousDate: previousDate ?? PreviousDate,
                currentDate: currentDate ?? CurrentDate,
                housingByHouseholdId: housingByHouseholdId ?? new Dictionary<HouseholdId, HousingStatus>(),
                residentialBuildingByHouseholdId: residentialBuildingByHouseholdId ??
                                                  new Dictionary<HouseholdId, ResidentialBuildingId?>(),
                financialStressByHouseholdId: financialStressByHouseholdId ??
                                              new Dictionary<HouseholdId,
                                                  CityPopulationHouseholdFinancialStressState>(),
                commuteRoutingService: commuteRoutingService ?? new RecordingCommuteRoutingService(),
                cancellationToken: CancellationToken.None,
                householdPressurePolicy: householdPressurePolicy ?? new CityHouseholdPressurePolicy());
        }

        private static IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<Person>> CreateResidentsMap(
            params Person[] residents)
        {
            return new Dictionary<HouseholdId, IReadOnlyCollection<Person>>
            {
                [TestHouseholdId] = residents
            };
        }

        private static Person CreateResident(LifeStatus lifeStatus = LifeStatus.Alive)
        {
            return CreatePerson(
                personId: Guid.NewGuid(),
                householdId: TestHouseholdId.Value,
                lifeStatus: lifeStatus,
                currentDate: CurrentDate);
        }

        private static Person CreateEmployedResident(CityAnchorId workplaceAnchorId)
        {
            return CreatePerson(
                personId: Guid.NewGuid(),
                householdId: TestHouseholdId.Value,
                currentDate: CurrentDate,
                employmentStatus: EmploymentStatus.Employed,
                job: new Job(
                    workplaceId: WorkplaceId.From(Guid.NewGuid()),
                    title: "Engineer",
                    workplaceAnchorId: workplaceAnchorId));
        }

        private static CityPopulationHouseholdFinancialStressState CreateFinancialStressState()
        {
            var evaluatedAtUtc = new DateTimeOffset(
                year: 2048,
                month: 5,
                day: 6,
                hour: 0,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            return CityPopulationHouseholdFinancialStressState.Create(
                cityId: TestCityId,
                householdId: TestHouseholdId,
                overdueObligationCount: 3,
                overdueRentCount: 1,
                overdueUtilityCount: 1,
                arrearsObligationCount: 1,
                serviceCutoffCount: 1,
                evictionNoticeCount: 1,
                evictionEligibleCount: 1,
                oldestOverdueAgeDays: 60,
                totalOverdueAmount: 250m,
                distressScore: 0.65m,
                lastEvaluatedAtUtc: evaluatedAtUtc,
                updatedAtUtc: evaluatedAtUtc);
        }

        private static PressureSnapshot Capture(Person resident)
        {
            return new PressureSnapshot(
                Happiness: resident.Happiness.Value,
                Energy: resident.Energy.Value,
                Stress: resident.Stress.Value,
                SocialNeed: resident.SocialNeed.Value);
        }

        private static CityAnchorId CreateAnchorId()
        {
            return CityAnchorId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        }

        private sealed record PressureSnapshot(
            int Happiness,
            int Energy,
            int Stress,
            int SocialNeed);

        private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
        {
            private readonly Dictionary<CityAnchorId, CityPopulationCommuteContext> _anchorContexts = [];

            public List<CityPopulationCommuteRouteRequest> PreloadRequests { get; } = [];
            public List<CityAnchorId?> ResolvedAnchorIds { get; } = [];

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
                ResolvedAnchorIds.Add(destinationAnchorId);

                return Task.FromResult(
                    destinationAnchorId.HasValue &&
                    _anchorContexts.TryGetValue(
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

            public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? healthcareAnchorId,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public void SetAnchorContext(
                CityAnchorId anchorId,
                CityPopulationCommuteContext context)
            {
                _anchorContexts[anchorId] = context;
            }
        }
    }
}
