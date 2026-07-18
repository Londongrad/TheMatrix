using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class ResidentTimeProgressionStepTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        private static readonly HouseholdId TestHouseholdId =
            HouseholdId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        private static readonly ResidentialBuildingId TestResidentialBuildingId = ResidentialBuildingId.From(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

        private static readonly DateOnly PreviousDate = new(
            year: 2048,
            month: 5,
            day: 1);

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 2);

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2048,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task ApplyAsync_WhenResidentIsDead_ReturnsFalseAndDoesNotCallRouting()
        {
            PersonEntity resident = CreateResident(lifeStatus: LifeStatus.Deceased);
            var routingService = new RecordingCommuteRoutingService();

            bool changed = await ApplyAsync(
                resident: resident,
                householdsById: CreateHouseholdsMap(resident.HouseholdId),
                residentsByHouseholdId: CreateResidentsMap(resident),
                workplaceAnchors:
                [
                    CreateAnchor(
                        anchorId: CreateAnchorId(1),
                        type: CityAnchorType.Workplace)
                ],
                commuteRoutingService: routingService);

            Assert.False(changed);
            Assert.Equal(
                expected: 0,
                actual: routingService.PreloadCallCount);
            Assert.Empty(routingService.ResolvedAnchorIds);
        }

        [Fact]
        public async Task ApplyAsync_WhenHouseholdIsMissing_ReturnsFalseAndDoesNotCallRouting()
        {
            PersonEntity resident = CreateResident();
            var routingService = new RecordingCommuteRoutingService();

            bool changed = await ApplyAsync(
                resident: resident,
                residentsByHouseholdId: CreateResidentsMap(resident),
                residentialBuildingByHouseholdId: CreateResidentialBuildingMap(resident.HouseholdId),
                workplaceAnchors:
                [
                    CreateAnchor(
                        anchorId: CreateAnchorId(3),
                        type: CityAnchorType.Workplace)
                ],
                commuteRoutingService: routingService);

            Assert.False(changed);
            Assert.Equal(
                expected: 0,
                actual: routingService.PreloadCallCount);
            Assert.Empty(routingService.ResolvedAnchorIds);
        }

        [Fact]
        public async Task ApplyAsync_WhenHouseholdExists_RanksOnlyWorkplaceAnchors()
        {
            PersonEntity resident = CreateResident(
                birthDate: new DateOnly(
                    year: 2025,
                    month: 5,
                    day: 2));
            CityPopulationAnchorCatalogItem workplaceA = CreateAnchor(
                anchorId: CreateAnchorId(7),
                type: CityAnchorType.Workplace);
            CityPopulationAnchorCatalogItem workplaceB = CreateAnchor(
                anchorId: CreateAnchorId(8),
                type: CityAnchorType.Workplace);
            var routingService = new RecordingCommuteRoutingService();

            await ApplyAsync(
                resident: resident,
                householdsById: CreateHouseholdsMap(resident.HouseholdId),
                residentsByHouseholdId: CreateResidentsMap(resident),
                residentialBuildingByHouseholdId: CreateResidentialBuildingMap(resident.HouseholdId),
                workplaceAnchors:
                [
                    workplaceA,
                    workplaceB
                ],
                commuteRoutingService: routingService);

            Assert.Equal(
                expected: 1,
                actual: routingService.PreloadCallCount);
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
                    workplaceA.CityAnchorId,
                    workplaceB.CityAnchorId
                ],
                actualArray: routingService.PreloadRequests.Select(request => request.DestinationAnchorId)
                   .ToArray());
            Assert.Equal(
                expected:
                [
                    workplaceA.CityAnchorId,
                    workplaceB.CityAnchorId
                ],
                actual: routingService.ResolvedAnchorIds.ToArray());
        }

        [Fact]
        public async Task ApplyAsync_WhenResidentialBuildingIsMissing_DoesNotRouteAnchors()
        {
            PersonEntity resident = CreateResident(
                birthDate: new DateOnly(
                    year: 2025,
                    month: 5,
                    day: 2));
            var routingService = new RecordingCommuteRoutingService();

            await ApplyAsync(
                resident: resident,
                householdsById: CreateHouseholdsMap(resident.HouseholdId),
                residentsByHouseholdId: CreateResidentsMap(resident),
                workplaceAnchors:
                [
                    CreateAnchor(
                        anchorId: CreateAnchorId(9),
                        type: CityAnchorType.Workplace)
                ],
                commuteRoutingService: routingService);

            Assert.Equal(
                expected: 0,
                actual: routingService.PreloadCallCount);
            Assert.Empty(routingService.PreloadRequests);
            Assert.Empty(routingService.ResolvedAnchorIds);
        }

        [Fact]
        public async Task ApplyAsync_WhenSeniorResidentIsEmployed_RetiresResidentAndReturnsTrue()
        {
            PersonEntity resident = CreateResident(
                birthDate: new DateOnly(
                    year: 1970,
                    month: 5,
                    day: 2),
                employmentStatus: EmploymentStatus.Employed,
                creationDate: new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 2),
                job: new Job(
                    workplaceId: WorkplaceId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                    title: "Engineer",
                    workplaceAnchorId: null));

            bool changed = await ApplyAsync(
                resident: resident,
                householdsById: CreateHouseholdsMap(resident.HouseholdId),
                residentsByHouseholdId: CreateResidentsMap(resident));

            Assert.True(changed);
            Assert.Equal(
                expected: EmploymentStatus.Retired,
                actual: resident.Employment.Status);
        }

        [Fact]
        public async Task ApplyAsync_WhenSeniorResidentIsUnemployed_DoesNotChangeEmployment()
        {
            PersonEntity resident = CreateResident(
                birthDate: new DateOnly(
                    year: 1970,
                    month: 5,
                    day: 2),
                employmentStatus: EmploymentStatus.Unemployed,
                creationDate: new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 2));

            bool changed = await ApplyAsync(
                resident: resident,
                householdsById: CreateHouseholdsMap(resident.HouseholdId),
                residentsByHouseholdId: CreateResidentsMap(resident));

            Assert.False(changed);
            Assert.Equal(
                expected: EmploymentStatus.Unemployed,
                actual: resident.Employment.Status);
        }

        [Fact]
        public async Task ApplyAsync_WhenNonSeniorResidentHasNoPlacementOpportunities_ReturnsFalse()
        {
            PersonEntity resident = CreateResident(
                birthDate: new DateOnly(
                    year: 2018,
                    month: 5,
                    day: 2),
                employmentStatus: EmploymentStatus.Retired);
            EmploymentStatus initialStatus = resident.Employment.Status;

            bool changed = await ApplyAsync(
                resident: resident,
                householdsById: CreateHouseholdsMap(resident.HouseholdId),
                residentsByHouseholdId: CreateResidentsMap(resident),
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [resident.HouseholdId] = HousingStatus.Housed
                });

            Assert.False(changed);
            Assert.Equal(
                expected: initialStatus,
                actual: resident.Employment.Status);
        }

        private static Task<bool> ApplyAsync(
            PersonEntity resident,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity>? householdsById = null,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>? residentsByHouseholdId = null,
            IReadOnlyDictionary<HouseholdId, HousingStatus>? housingByHouseholdId = null,
            IReadOnlyDictionary<HouseholdId, DistrictId?>? districtByHouseholdId = null,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?>? residentialBuildingByHouseholdId = null,
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>? employerStressByWorkplaceId =
                null,
            CityPopulationCostOfLivingState? costOfLivingState = null,
            IReadOnlyCollection<CityPopulationAnchorCatalogItem>? workplaceAnchors = null,
            IDictionary<string, List<Job>>? workplacePools = null,
            RecordingCommuteRoutingService? commuteRoutingService = null,
            DateOnly? previousDate = null,
            DateOnly? currentDate = null)
        {
            var anchorSelectionPolicy = new CityPopulationAnchorSelectionPolicy();

            return ResidentTimeProgressionStep.ApplyAsync(
                cityId: TestCityId,
                person: resident,
                householdsById: householdsById ?? new Dictionary<HouseholdId, HouseholdEntity>(),
                residentsByHouseholdId: residentsByHouseholdId ??
                                        new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>(),
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                economicContextsByResidentId: new Dictionary<PersonId, CityResidentEconomicContext>(),
                previousDate: previousDate ?? PreviousDate,
                currentDate: currentDate ?? CurrentDate,
                housingByHouseholdId: housingByHouseholdId ?? new Dictionary<HouseholdId, HousingStatus>(),
                districtByHouseholdId: districtByHouseholdId ?? new Dictionary<HouseholdId, DistrictId?>(),
                residentialBuildingByHouseholdId: residentialBuildingByHouseholdId ??
                                                  new Dictionary<HouseholdId, ResidentialBuildingId?>(),
                employerStressByWorkplaceId: employerStressByWorkplaceId ??
                                             new Dictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>(),
                costOfLivingState: costOfLivingState,
                employmentAutonomyPolicy: new CityEmploymentAutonomyPolicy(
                    contentCatalog: new TestPopulationGenerationContentCatalog(),
                    householdEconomyPolicy: CreateHouseholdEconomyPolicy(),
                    anchorSelectionPolicy: anchorSelectionPolicy),
                workplaceAnchors: workplaceAnchors ?? [],
                workplacePools: workplacePools ?? new Dictionary<string, List<Job>>(StringComparer.OrdinalIgnoreCase),
                commuteRoutingService: commuteRoutingService ?? new RecordingCommuteRoutingService(),
                cancellationToken: CancellationToken.None);
        }

        private static IReadOnlyDictionary<HouseholdId, HouseholdEntity> CreateHouseholdsMap(HouseholdId householdId)
        {
            return new Dictionary<HouseholdId, HouseholdEntity>
            {
                [householdId] = CreateHousehold(householdId)
            };
        }

        private static IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> CreateResidentsMap(
            params PersonEntity[] residents)
        {
            return new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>
            {
                [residents[0].HouseholdId] = residents
            };
        }

        private static IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> CreateResidentialBuildingMap(
            HouseholdId householdId)
        {
            return new Dictionary<HouseholdId, ResidentialBuildingId?>
            {
                [householdId] = TestResidentialBuildingId
            };
        }

        private static HouseholdEntity CreateHousehold(HouseholdId householdId)
        {
            return HouseholdEntity.Create(
                id: householdId,
                size: HouseholdSize.From(1),
                createdAtUtc: CreatedAtUtc,
                cashReserve: Money.FromDecimal(100m));
        }

        private static PersonEntity CreateResident(
            LifeStatus lifeStatus = LifeStatus.Alive,
            DateOnly? birthDate = null,
            EmploymentStatus employmentStatus = EmploymentStatus.Unemployed,
            DateOnly? creationDate = null,
            Job? job = null)
        {
            return CreatePerson(
                personId: Guid.NewGuid(),
                householdId: TestHouseholdId.Value,
                lifeStatus: lifeStatus,
                birthDate: birthDate ??
                new DateOnly(
                    year: 1990,
                    month: 5,
                    day: 2),
                currentDate: creationDate ?? CurrentDate,
                employmentStatus: employmentStatus,
                job: job);
        }

        private static CityPopulationAnchorCatalogItem CreateAnchor(
            CityAnchorId anchorId,
            CityAnchorType type)
        {
            return CityPopulationAnchorCatalogItem.Create(
                cityId: TestCityId,
                cityAnchorId: anchorId,
                districtId: DistrictId.From(Guid.NewGuid()),
                accessRoadNodeId: RoadNodeId.From(Guid.NewGuid()),
                name: $"{type} Anchor",
                type: type,
                capacity: 100,
                positionX: 0m,
                positionY: 0m,
                createdAtUtc: CreatedAtUtc);
        }

        private static CityAnchorId CreateAnchorId(int index)
        {
            return CityAnchorId.From(Guid.Parse($"dddddddd-dddd-dddd-dddd-{index:000000000000}"));
        }

        private static CityHouseholdEconomyPolicy CreateHouseholdEconomyPolicy()
        {
            return new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                householdCashflowPolicy: new CityHouseholdCashflowPolicy());
        }

        private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
        {
            private readonly Dictionary<CityAnchorId, CityPopulationCommuteContext> _anchorContexts = [];

            public List<CityPopulationCommuteRouteRequest> PreloadRequests { get; } = [];
            public List<CityAnchorId?> ResolvedAnchorIds { get; } = [];
            public int PreloadCallCount { get; private set; }

            public Task PreloadAnchorCommutesAsync(
                Guid cityId,
                IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
                CancellationToken cancellationToken)
            {
                PreloadCallCount++;
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
                PersonEntity resident,
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

        private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
        {
            public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
            public IReadOnlyList<string> FemaleFirstNames => ["Anna"];
            public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames => [];

            public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
            [
                new(
                    Title: "Engineer",
                    Weight: 1)
            ];
        }
    }
}
