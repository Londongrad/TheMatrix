using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.Common
{
    public sealed class ClassicCityHousingOpportunityPlannerTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        private static readonly DateOnly CurrentDate = new(
            year: 2030,
            month: 1,
            day: 1);

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2030,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public void
            BuildHousingOpportunityPool_FiltersHousedPlacementsWithDistrictAndBuildingAndPreservesFirstOccurrenceOrder()
        {
            DistrictId firstDistrictId = CreateDistrictId(1);
            ResidentialBuildingId firstBuildingId = CreateResidentialBuildingId(1);
            DistrictId secondDistrictId = CreateDistrictId(2);
            ResidentialBuildingId secondBuildingId = CreateResidentialBuildingId(2);
            ClassicCityHouseholdPlacement[] placements =
            [
                ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: CreateHouseholdId(1),
                    cityId: TestCityId,
                    districtId: firstDistrictId,
                    residentialBuildingId: firstBuildingId),
                ClassicCityHouseholdPlacement.CreateHomeless(
                    householdId: CreateHouseholdId(2),
                    cityId: TestCityId),
                ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: CreateHouseholdId(3),
                    cityId: TestCityId,
                    districtId: firstDistrictId,
                    residentialBuildingId: firstBuildingId),
                ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: CreateHouseholdId(4),
                    cityId: TestCityId,
                    districtId: secondDistrictId,
                    residentialBuildingId: secondBuildingId)
            ];

            List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> pool =
                ClassicCityHousingOpportunityPlanner.BuildHousingOpportunityPool(placements);

            Assert.Equal(
                expected: 2,
                actual: pool.Count);
            Assert.Equal(
                expected: firstDistrictId,
                actual: pool[0].DistrictId);
            Assert.Equal(
                expected: firstBuildingId,
                actual: pool[0].ResidentialBuildingId);
            Assert.Equal(
                expected: secondDistrictId,
                actual: pool[1].DistrictId);
            Assert.Equal(
                expected: secondBuildingId,
                actual: pool[1].ResidentialBuildingId);
        }

        [Fact]
        public void ResolveDistrictUtilityConditions_WhenDistrictIsNull_ReturnsNull()
        {
            Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> snapshots = new()
            {
                [CreateDistrictId(1)] = CreateUtilitySnapshot(CreateDistrictId(1))
            };

            CityDistrictUtilityConditionsSnapshot? result =
                ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                    districtId: null,
                    districtUtilityConditionsByDistrictId: snapshots);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveDistrictUtilityConditions_WhenDistrictIsMissing_ReturnsNull()
        {
            Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> snapshots = new()
            {
                [CreateDistrictId(1)] = CreateUtilitySnapshot(CreateDistrictId(1))
            };

            CityDistrictUtilityConditionsSnapshot? result =
                ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                    districtId: CreateDistrictId(2),
                    districtUtilityConditionsByDistrictId: snapshots);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveDistrictUtilityConditions_WhenDistrictExists_ReturnsMatchingSnapshot()
        {
            DistrictId districtId = CreateDistrictId(1);
            CityDistrictUtilityConditionsSnapshot snapshot = CreateUtilitySnapshot(districtId);
            Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> snapshots = new()
            {
                [districtId] = snapshot
            };

            CityDistrictUtilityConditionsSnapshot? result =
                ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                    districtId: districtId,
                    districtUtilityConditionsByDistrictId: snapshots);

            Assert.Same(
                expected: snapshot,
                actual: result);
        }

        [Fact]
        public void ResolveDistrictHousingStabilityContribution_WhenSnapshotIsNull_ReturnsNeutralFallback()
        {
            decimal result = ClassicCityHousingOpportunityPlanner.ResolveDistrictHousingStabilityContribution(null);

            Assert.Equal(
                expected: 0.55m,
                actual: result);
        }

        [Fact]
        public void ResolveDistrictHousingStabilityContribution_CalculatesRoundedClampedScore()
        {
            CityDistrictUtilityConditionsSnapshot snapshot = CreateUtilitySnapshot(
                districtId: CreateDistrictId(1),
                heatingCoverageIndex: 0.90m,
                waterCoverageIndex: 0.75m,
                powerCoverageIndex: 0.80m,
                sanitationCoverageIndex: 0.70m,
                dispatchReadinessIndex: 0.80m,
                pressureIndex: 0.20m,
                coordinationDifficultyIndex: 0.25m,
                restorationPriorityIndex: 0.40m);
            decimal expectedRaw = (snapshot.UtilityIncidentDispatchReadinessIndex * 0.22m) +
                                  ((1m - snapshot.UtilityIncidentPressureIndex) * 0.26m) +
                                  ((1m - snapshot.UtilityIncidentCoordinationDifficultyIndex) * 0.14m) +
                                  ((1m - snapshot.UtilityIncidentRestorationPriorityIndex) * 0.12m) +
                                  (snapshot.HeatingCoverageIndex * 0.08m) +
                                  (snapshot.WaterCoverageIndex * 0.08m) +
                                  (snapshot.PowerCoverageIndex * 0.06m) +
                                  (snapshot.SanitationCoverageIndex * 0.04m);
            decimal expected = decimal.Round(
                d: Math.Clamp(
                    value: expectedRaw,
                    min: 0m,
                    max: 1m),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);

            decimal result = ClassicCityHousingOpportunityPlanner.ResolveDistrictHousingStabilityContribution(snapshot);

            Assert.Equal(
                expected: expected,
                actual: result);
            Assert.InRange(
                actual: result,
                low: 0m,
                high: 1m);
        }

        [Fact]
        public void ResolveHousingOpportunityContribution_WhenRouteIsAccessible_RewardsShortRoutes()
        {
            CityPopulationCommuteContext commute = new(
                HasRouteData: true,
                IsAccessible: true,
                AccessibilityIndex: 0.8m,
                PassabilityIndex: 0.9m,
                EstimatedTravelTimeMinutes: 30m);
            decimal etaScore = 1m - (30m / 120m);
            decimal expected = ((0.8m * 0.65m) + (0.9m * 0.20m) + (etaScore * 0.15m)) * 1.20m;

            decimal result = ClassicCityHousingOpportunityPlanner.ResolveHousingOpportunityContribution(
                commute: commute,
                weight: 1.20m);

            Assert.Equal(
                expected: expected,
                actual: result);
        }

        [Fact]
        public void ResolveHousingOpportunityContribution_WhenRouteIsInaccessible_AppliesPenalty()
        {
            CityPopulationCommuteContext commute = new(
                HasRouteData: true,
                IsAccessible: false,
                AccessibilityIndex: 0.8m,
                PassabilityIndex: 0.9m,
                EstimatedTravelTimeMinutes: 30m);
            decimal etaScore = 1m - (30m / 120m);
            decimal rawWeightedScore = ((0.8m * 0.65m) + (0.9m * 0.20m) + (etaScore * 0.15m)) * 1.20m;
            decimal expected = rawWeightedScore * 0.30m;

            decimal result = ClassicCityHousingOpportunityPlanner.ResolveHousingOpportunityContribution(
                commute: commute,
                weight: 1.20m);

            Assert.Equal(
                expected: expected,
                actual: result);
        }

        [Fact]
        public void ResolveHousingOpportunityContribution_WhenEtaIsMissing_TreatsEtaScoreAsOne()
        {
            CityPopulationCommuteContext commute = new(
                HasRouteData: true,
                IsAccessible: true,
                AccessibilityIndex: 0.8m,
                PassabilityIndex: 0.9m,
                EstimatedTravelTimeMinutes: null);
            decimal expected = ((0.8m * 0.65m) + (0.9m * 0.20m) + (1m * 0.15m)) * 1.20m;

            decimal result = ClassicCityHousingOpportunityPlanner.ResolveHousingOpportunityContribution(
                commute: commute,
                weight: 1.20m);

            Assert.Equal(
                expected: expected,
                actual: result);
        }

        [Fact]
        public void GetStableInt_IsDeterministicAndWithinRange()
        {
            var householdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

            int first = ClassicCityHousingOpportunityPlanner.GetStableInt(
                householdId: householdId,
                currentDate: CurrentDate,
                salt: 1_123,
                modulus: 12);
            int second = ClassicCityHousingOpportunityPlanner.GetStableInt(
                householdId: householdId,
                currentDate: CurrentDate,
                salt: 1_123,
                modulus: 12);

            Assert.Equal(
                expected: first,
                actual: second);
            Assert.InRange(
                actual: first,
                low: 0,
                high: 11);
        }

        [Fact]
        public void GetStableInt_WhenModulusIsNonPositive_ReturnsZero()
        {
            var householdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

            int zeroModulusResult = ClassicCityHousingOpportunityPlanner.GetStableInt(
                householdId: householdId,
                currentDate: CurrentDate,
                salt: 1_123,
                modulus: 0);
            int negativeModulusResult = ClassicCityHousingOpportunityPlanner.GetStableInt(
                householdId: householdId,
                currentDate: CurrentDate,
                salt: 1_123,
                modulus: -1);

            Assert.Equal(
                expected: 0,
                actual: zeroModulusResult);
            Assert.Equal(
                expected: 0,
                actual: negativeModulusResult);
        }

        [Fact]
        public async Task SelectHousingOpportunityAsync_ChoosesHighestScoringCandidateInDeterministicWindow()
        {
            HouseholdId householdId = CreateHouseholdId(1);
            CityAnchorId workplaceAnchorId = CreateCityAnchorId(1);
            ResidentialBuildingId lowScoreBuildingId = CreateResidentialBuildingId(1);
            ResidentialBuildingId highScoreBuildingId = CreateResidentialBuildingId(2);
            ResidentialBuildingId mediumScoreBuildingId = CreateResidentialBuildingId(3);
            List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> housingPool =
            [
                (CreateDistrictId(1), lowScoreBuildingId),
                (CreateDistrictId(2), highScoreBuildingId),
                (CreateDistrictId(3), mediumScoreBuildingId)
            ];
            Person resident = CreateResident(
                personId: CreatePersonId(1),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1990,
                    month: 1,
                    day: 1),
                job: CreateJob(workplaceAnchorId));
            var routingService = new RecordingCommuteRoutingService();
            routingService.AnchorContextsByBuilding[lowScoreBuildingId] = new CityPopulationCommuteContext(
                HasRouteData: true,
                IsAccessible: true,
                AccessibilityIndex: 0.20m,
                PassabilityIndex: 0.20m,
                EstimatedTravelTimeMinutes: 100m);
            routingService.AnchorContextsByBuilding[highScoreBuildingId] = new CityPopulationCommuteContext(
                HasRouteData: true,
                IsAccessible: true,
                AccessibilityIndex: 0.95m,
                PassabilityIndex: 0.90m,
                EstimatedTravelTimeMinutes: 10m);
            routingService.AnchorContextsByBuilding[mediumScoreBuildingId] = new CityPopulationCommuteContext(
                HasRouteData: true,
                IsAccessible: true,
                AccessibilityIndex: 0.55m,
                PassabilityIndex: 0.60m,
                EstimatedTravelTimeMinutes: 50m);

            (DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId) result =
                await ClassicCityHousingOpportunityPlanner.SelectHousingOpportunityAsync(
                    cityId: TestCityId,
                    householdId: householdId,
                    currentDate: CurrentDate,
                    housingPool: housingPool,
                    householdResidents: [resident],
                    hospitalAnchors: [],
                    districtUtilityConditionsByDistrictId:
                    new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
                    anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
                    commuteRoutingService: routingService,
                    cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: highScoreBuildingId,
                actual: result.ResidentialBuildingId);
            Assert.Equal(
                expected: 3,
                actual: routingService.AnchorRequests.Count);
            Assert.Contains(
                expected: lowScoreBuildingId,
                collection: routingService.AnchorRequests.Select(x => x.ResidentialBuildingId));
            Assert.Contains(
                expected: highScoreBuildingId,
                collection: routingService.AnchorRequests.Select(x => x.ResidentialBuildingId));
            Assert.Contains(
                expected: mediumScoreBuildingId,
                collection: routingService.AnchorRequests.Select(x => x.ResidentialBuildingId));
        }

        [Fact]
        public async Task SelectHousingOpportunityAsync_RespectsCandidateWindowLimitOfTwelve()
        {
            HouseholdId householdId = CreateHouseholdId(1);
            CityAnchorId workplaceAnchorId = CreateCityAnchorId(1);
            List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> housingPool = Enumerable
               .Range(
                    start: 1,
                    count: 20)
               .Select(index => (CreateDistrictId(index), CreateResidentialBuildingId(index)))
               .ToList();
            Person resident = CreateResident(
                personId: CreatePersonId(1),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1990,
                    month: 1,
                    day: 1),
                job: CreateJob(workplaceAnchorId));
            var routingService = new RecordingCommuteRoutingService
            {
                DefaultAnchorContext = new CityPopulationCommuteContext(
                    HasRouteData: true,
                    IsAccessible: true,
                    AccessibilityIndex: 0.60m,
                    PassabilityIndex: 0.60m,
                    EstimatedTravelTimeMinutes: 60m)
            };

            (DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId) result =
                await ClassicCityHousingOpportunityPlanner.SelectHousingOpportunityAsync(
                    cityId: TestCityId,
                    householdId: householdId,
                    currentDate: CurrentDate,
                    housingPool: housingPool,
                    householdResidents: [resident],
                    hospitalAnchors: [],
                    districtUtilityConditionsByDistrictId:
                    new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
                    anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
                    commuteRoutingService: routingService,
                    cancellationToken: CancellationToken.None);

            ResidentialBuildingId[] evaluatedBuildings = routingService.AnchorRequests
               .Select(x => x.ResidentialBuildingId)
               .Where(x => x.HasValue)
               .Select(x => x!.Value)
               .Distinct()
               .ToArray();

            Assert.Equal(
                expected: 12,
                actual: evaluatedBuildings.Length);
            Assert.Contains(
                expected: result.ResidentialBuildingId,
                collection: evaluatedBuildings);
        }

        [Fact]
        public async Task SelectHousingOpportunityAsync_PreloadsCandidateWindowRoutesBeforeResolving()
        {
            HouseholdId householdId = CreateHouseholdId(1);
            CityAnchorId workplaceAnchorId = CreateCityAnchorId(1);
            List<(DistrictId DistrictId, ResidentialBuildingId ResidentialBuildingId)> housingPool =
            [
                (CreateDistrictId(1), CreateResidentialBuildingId(1)),
                (CreateDistrictId(2), CreateResidentialBuildingId(2)),
                (CreateDistrictId(3), CreateResidentialBuildingId(3))
            ];
            Person resident = CreateResident(
                personId: CreatePersonId(1),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1990,
                    month: 1,
                    day: 1),
                job: CreateJob(workplaceAnchorId));
            var routingService = new RecordingCommuteRoutingService
            {
                DefaultAnchorContext = new CityPopulationCommuteContext(
                    HasRouteData: true,
                    IsAccessible: true,
                    AccessibilityIndex: 0.60m,
                    PassabilityIndex: 0.60m,
                    EstimatedTravelTimeMinutes: 60m)
            };

            await ClassicCityHousingOpportunityPlanner.SelectHousingOpportunityAsync(
                cityId: TestCityId,
                householdId: householdId,
                currentDate: CurrentDate,
                housingPool: housingPool,
                householdResidents: [resident],
                hospitalAnchors: [],
                districtUtilityConditionsByDistrictId:
                new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
                anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
                commuteRoutingService: routingService,
                cancellationToken: CancellationToken.None);

            IReadOnlyCollection<CityPopulationCommuteRouteRequest> preloadRequests =
                Assert.Single(routingService.PreloadRequests);
            Assert.Equal(
                expected: 3,
                actual: preloadRequests.Count);
            foreach ((DistrictId _, ResidentialBuildingId residentialBuildingId) in housingPool)
                Assert.Contains(
                    collection: preloadRequests,
                    filter: request => request.ResidentialBuildingId == residentialBuildingId &&
                                       request.DestinationAnchorId == workplaceAnchorId &&
                                       request.Profile == CityPopulationCommuteRoutingProfiles.Pedestrian);
        }

        [Fact]
        public void SelectHousingAnchorResident_PrefersAdultOrSeniorThenOlderAgeThenLowerPersonId()
        {
            HouseholdId householdId = CreateHouseholdId(1);
            Person child = CreateResident(
                personId: CreatePersonId(3),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 2020,
                    month: 1,
                    day: 1));
            Person adult = CreateResident(
                personId: CreatePersonId(2),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1990,
                    month: 1,
                    day: 1));
            Person youngerAdult = CreateResident(
                personId: CreatePersonId(5),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1995,
                    month: 1,
                    day: 1));
            Person olderAdult = CreateResident(
                personId: CreatePersonId(4),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1975,
                    month: 1,
                    day: 1));
            Person lowerIdAdult = CreateResident(
                personId: CreatePersonId(6),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1990,
                    month: 1,
                    day: 1));
            Person higherIdAdult = CreateResident(
                personId: CreatePersonId(7),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 1990,
                    month: 1,
                    day: 1));

            Person adultOverChild = ClassicCityHousingOpportunityPlanner.SelectHousingAnchorResident(
                householdResidents:
                [
                    child,
                    adult
                ],
                currentDate: CurrentDate);
            Person olderOverYounger = ClassicCityHousingOpportunityPlanner.SelectHousingAnchorResident(
                householdResidents:
                [
                    youngerAdult,
                    olderAdult
                ],
                currentDate: CurrentDate);
            Person lowerIdOnTie = ClassicCityHousingOpportunityPlanner.SelectHousingAnchorResident(
                householdResidents:
                [
                    higherIdAdult,
                    lowerIdAdult
                ],
                currentDate: CurrentDate);

            Assert.Equal(
                expected: adult.Id,
                actual: adultOverChild.Id);
            Assert.Equal(
                expected: olderAdult.Id,
                actual: olderOverYounger.Id);
            Assert.Equal(
                expected: lowerIdAdult.Id,
                actual: lowerIdOnTie.Id);
        }

        private static CityDistrictUtilityConditionsSnapshot CreateUtilitySnapshot(
            DistrictId districtId,
            decimal heatingCoverageIndex = 0.80m,
            decimal waterCoverageIndex = 0.80m,
            decimal powerCoverageIndex = 0.80m,
            decimal sanitationCoverageIndex = 0.80m,
            decimal dispatchReadinessIndex = 0.70m,
            decimal pressureIndex = 0.30m,
            decimal coordinationDifficultyIndex = 0.20m,
            decimal restorationPriorityIndex = 0.20m)
        {
            return new CityDistrictUtilityConditionsSnapshot(
                DistrictId: districtId,
                HeatingCoverageIndex: heatingCoverageIndex,
                HeatingComfortStressIndex: 0.10m,
                WaterCoverageIndex: waterCoverageIndex,
                WaterDisruptionRiskIndex: 0.10m,
                PowerCoverageIndex: powerCoverageIndex,
                PowerOutageRiskIndex: 0.10m,
                SanitationCoverageIndex: sanitationCoverageIndex,
                SanitationContaminationRiskIndex: 0.10m,
                UtilityIncidentDispatchReadinessIndex: dispatchReadinessIndex,
                UtilityIncidentPressureIndex: pressureIndex,
                UtilityIncidentCoordinationDifficultyIndex: coordinationDifficultyIndex,
                UtilityIncidentRestorationPriorityIndex: restorationPriorityIndex);
        }

        private static Person CreateResident(
            PersonId personId,
            HouseholdId householdId,
            DateOnly birthDate,
            Job? job = null)
        {
            return Person.CreatePerson(
                id: personId,
                householdId: householdId,
                name: new PersonName(
                    firstName: "Resident",
                    lastName: personId.Value.ToString("N")[..8]),
                sex: Sex.Male,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                spouseId: null,
                educationLevel: EducationLevel.UpperSecondary,
                educationInstitutionId: null,
                educationInstitutionAnchorId: null,
                employmentStatus: job is null
                    ? EmploymentStatus.Unemployed
                    : EmploymentStatus.Employed,
                happinessLevel: HappinessLevel.From(50),
                energyLevel: EnergyLevel.From(70),
                stressLevel: StressLevel.From(20),
                socialNeedLevel: SocialNeedLevel.From(40),
                personality: Personality.Neutral(),
                birthDate: birthDate,
                healthLevel: HealthLevel.From(90),
                weight: BodyWeight.FromKilograms(72m),
                job: job,
                currentDate: CurrentDate,
                illness: IllnessInfo.Healthy());
        }

        private static Job CreateJob(CityAnchorId workplaceAnchorId)
        {
            return new Job(
                workplaceId: WorkplaceId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
                title: "Engineer",
                workplaceAnchorId: workplaceAnchorId);
        }

        private static HouseholdId CreateHouseholdId(int index)
        {
            return HouseholdId.From(
                CreateGuid(
                    prefix: "10000000",
                    index: index));
        }

        private static PersonId CreatePersonId(int index)
        {
            return PersonId.From(
                CreateGuid(
                    prefix: "20000000",
                    index: index));
        }

        private static DistrictId CreateDistrictId(int index)
        {
            return DistrictId.From(
                CreateGuid(
                    prefix: "30000000",
                    index: index));
        }

        private static ResidentialBuildingId CreateResidentialBuildingId(int index)
        {
            return ResidentialBuildingId.From(
                CreateGuid(
                    prefix: "40000000",
                    index: index));
        }

        private static CityAnchorId CreateCityAnchorId(int index)
        {
            return CityAnchorId.From(
                CreateGuid(
                    prefix: "50000000",
                    index: index));
        }

        private static Guid CreateGuid(
            string prefix,
            int index)
        {
            return Guid.Parse($"{prefix}-0000-0000-0000-{index:000000000000}");
        }

        private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
        {
            public Dictionary<ResidentialBuildingId, CityPopulationCommuteContext> AnchorContextsByBuilding { get; } =
                [];

            public List<(ResidentialBuildingId? ResidentialBuildingId, CityAnchorId? DestinationAnchorId)>
                AnchorRequests { get; } = [];

            public List<IReadOnlyCollection<CityPopulationCommuteRouteRequest>> PreloadRequests { get; } = [];

            public CityPopulationCommuteContext DefaultAnchorContext { get; set; } =
                CityPopulationCommuteContext.Neutral;

            public Task PreloadAnchorCommutesAsync(
                Guid cityId,
                IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
                CancellationToken cancellationToken)
            {
                PreloadRequests.Add(requests);
                return Task.CompletedTask;
            }

            public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? destinationAnchorId,
                CancellationToken cancellationToken)
            {
                AnchorRequests.Add((residentialBuildingId, destinationAnchorId));

                if (residentialBuildingId.HasValue &&
                    AnchorContextsByBuilding.TryGetValue(
                        key: residentialBuildingId.Value,
                        value: out CityPopulationCommuteContext? context))
                    return Task.FromResult(context);

                return Task.FromResult(DefaultAnchorContext);
            }

            public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(CityPopulationCommuteContext.Neutral);
            }

            public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                Person resident,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(CityPopulationCommuteContext.Neutral);
            }

            public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                CityAnchorId? healthcareAnchorId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(CityPopulationCommuteContext.Neutral);
            }
        }
    }
}
