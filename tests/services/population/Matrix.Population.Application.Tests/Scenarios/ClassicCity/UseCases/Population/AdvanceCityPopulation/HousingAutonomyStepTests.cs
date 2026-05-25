using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class HousingAutonomyStepTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly DateOnly PreviousDate = new(2047, 11, 1);
    private static readonly DateOnly CurrentDate = new(2048, 5, 2);
    private static readonly DateTimeOffset OccurredAtUtc = new(2048, 5, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EvaluatedAtUtc = new(2048, 5, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApplyAsync_WhenNoPlacementsExist_ReturnsZeroAndDoesNotMutate()
    {
        var resident = CreateResident(
            personId: CreateGuid(1),
            householdId: HouseholdId.From(CreateGuid(101)));
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult = []
        };
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await ApplyAsync(
            residentsById: CreateResidentsById(resident),
            householdWriteRepository: householdWriteRepository,
            activityEntries: activityEntries);

        Assert.Equal(0, affected);
        Assert.Equal(TestCityId, householdWriteRepository.RequestedCityId);
        Assert.Empty(activityEntries);
        Assert.Empty(householdWriteRepository.UpdatedHouseholds);
        Assert.Empty(householdWriteRepository.AddedHouseholds);
    }

    [Fact]
    public async Task ApplyAsync_WhenPolicyPlansNoDecisions_ReturnsZero()
    {
        HouseholdId householdId = HouseholdId.From(CreateGuid(201));
        var resident = CreateResident(
            personId: CreateGuid(2),
            householdId: householdId);
        ClassicCityHouseholdPlacement placement = CreateHousedPlacement(
            householdId: householdId,
            districtId: CreateDistrictId(201),
            residentialBuildingId: CreateResidentialBuildingId(201));
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult = [placement],
            HouseholdsByCityResult = [CreateHousehold(householdId, cashReserve: 1_000m)]
        };
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await ApplyAsync(
            residentsById: CreateResidentsById(resident),
            previousDate: CurrentDate,
            currentDate: CurrentDate,
            householdWriteRepository: householdWriteRepository,
            activityEntries: activityEntries);

        Assert.Equal(0, affected);
        Assert.Equal(HousingStatus.Housed, placement.HousingStatus);
        Assert.Empty(activityEntries);
    }

    [Fact]
    public async Task ApplyAsync_WhenForcedEvictionDecisionIsPlanned_LosesHousingAndWritesActivity()
    {
        HouseholdId householdId = HouseholdId.From(CreateGuid(301));
        var resident = CreateResident(
            personId: CreateGuid(3),
            householdId: householdId);
        resident.ChangeHealth(-60, CurrentDate);
        resident.ChangeEnergy(-50);
        resident.ChangeStress(55);
        resident.ChangeHappiness(-35);
        ClassicCityHouseholdPlacement placement = CreateHousedPlacement(
            householdId: householdId,
            districtId: CreateDistrictId(301),
            residentialBuildingId: CreateResidentialBuildingId(301));
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult = [placement],
            HouseholdsByCityResult = [CreateHousehold(householdId, cashReserve: -500m)]
        };
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await ApplyAsync(
            residentsById: CreateResidentsById(resident),
            householdWriteRepository: householdWriteRepository,
            financialStressByHouseholdId: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
            {
                [householdId] = CreateFinancialStressState(
                    householdId: householdId,
                    distressScore: 0.85m,
                    oldestOverdueAgeDays: 100)
            },
            serviceQualityState: CreateServiceQualityState(housingSupportIndex: 1m),
            activityEntries: activityEntries);

        Assert.Equal(1, affected);
        Assert.Equal(HousingStatus.Homeless, placement.HousingStatus);
        Assert.Null(placement.DistrictId);
        Assert.Null(placement.ResidentialBuildingId);
        CityPopulationActivityWriteModel activity = Assert.Single(activityEntries);
        Assert.Equal(CityPopulationActivityEventType.HouseholdLostHousing, activity.EventType);
        Assert.Equal(CityPopulationActivitySource.Autonomy, activity.Source);
        Assert.Equal(CityPopulationActivitySeverity.Warning, activity.Severity);
        Assert.Equal(TestCityId.Value, activity.CityId);
        Assert.Equal(CurrentDate, activity.CurrentDate);
        Assert.Equal(OccurredAtUtc, activity.OccurredAtUtc);
        Assert.Equal(resident.Id.Value, activity.PrimaryResidentId);
    }

    [Fact]
    public async Task ApplyAsync_WhenFindHousingDecisionIsPlanned_RelocatesHouseholdAndWritesActivity()
    {
        var policy = CreatePolicy();
        (PersonEntity resident, HouseholdId householdId) = FindStableFindHousingHousehold(policy);
        DistrictId opportunityDistrictId = CreateDistrictId(401);
        ResidentialBuildingId opportunityBuildingId = CreateResidentialBuildingId(401);
        ClassicCityHouseholdPlacement homelessPlacement = ClassicCityHouseholdPlacement.CreateHomeless(
            householdId: householdId,
            cityId: TestCityId);
        ClassicCityHouseholdPlacement opportunityPlacement = CreateHousedPlacement(
            householdId: HouseholdId.From(CreateGuid(402)),
            districtId: opportunityDistrictId,
            residentialBuildingId: opportunityBuildingId);
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult = [homelessPlacement, opportunityPlacement],
            HouseholdsByCityResult =
            [
                CreateHousehold(householdId, cashReserve: 20_000m),
                CreateHousehold(opportunityPlacement.HouseholdId, cashReserve: 5_000m)
            ]
        };
        var commuteRoutingService = new RecordingCommuteRoutingService
        {
            DefaultAnchorContext = new CityPopulationCommuteContext(
                HasRouteData: true,
                IsAccessible: true,
                AccessibilityIndex: 0.95m,
                PassabilityIndex: 0.90m,
                EstimatedTravelTimeMinutes: 15m)
        };
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await ApplyAsync(
            residentsById: CreateResidentsById(resident),
            householdWriteRepository: householdWriteRepository,
            housingAutonomyPolicy: policy,
            serviceQualityState: CreateServiceQualityState(housingSupportIndex: 3m),
            commuteRoutingService: commuteRoutingService,
            activityEntries: activityEntries);

        Assert.Equal(1, affected);
        Assert.Equal(HousingStatus.Housed, homelessPlacement.HousingStatus);
        Assert.Equal(opportunityDistrictId, homelessPlacement.DistrictId);
        Assert.Equal(opportunityBuildingId, homelessPlacement.ResidentialBuildingId);
        Assert.NotEmpty(commuteRoutingService.PreloadRequests);
        Assert.Contains(
            commuteRoutingService.PreloadRequests.SelectMany(x => x),
            request => request.ResidentialBuildingId == opportunityBuildingId &&
                       request.Profile == CityPopulationCommuteRoutingProfiles.Pedestrian);
        Assert.Contains(
            commuteRoutingService.AnchorRequests,
            request => request.ResidentialBuildingId == opportunityBuildingId);

        CityPopulationActivityWriteModel activity = Assert.Single(activityEntries);
        Assert.Equal(CityPopulationActivityEventType.HouseholdFoundHousing, activity.EventType);
        Assert.Equal(CityPopulationActivitySource.Autonomy, activity.Source);
        Assert.Equal(CityPopulationActivitySeverity.Success, activity.Severity);
        Assert.Equal(TestCityId.Value, activity.CityId);
        Assert.Equal(CurrentDate, activity.CurrentDate);
        Assert.Equal(OccurredAtUtc, activity.OccurredAtUtc);
        Assert.Equal(resident.Id.Value, activity.PrimaryResidentId);
    }

    private static Task<int> ApplyAsync(
        IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
        FakeHouseholdWriteRepository householdWriteRepository,
        DateOnly? previousDate = null,
        DateOnly? currentDate = null,
        IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>? financialStressByHouseholdId = null,
        CityPopulationCostOfLivingState? costOfLivingState = null,
        CityPopulationServiceQualityState? serviceQualityState = null,
        IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>? districtUtilityConditionsByDistrictId = null,
        CityHousingAutonomyPolicy? housingAutonomyPolicy = null,
        FakeCityPopulationAnchorCatalogRepository? anchorCatalogRepository = null,
        RecordingCommuteRoutingService? commuteRoutingService = null,
        ICollection<CityPopulationActivityWriteModel>? activityEntries = null)
    {
        return HousingAutonomyStep.ApplyAsync(
            cityId: TestCityId,
            residentsById: residentsById,
            previousDate: previousDate ?? PreviousDate,
            currentDate: currentDate ?? CurrentDate,
            householdWriteRepository: householdWriteRepository,
            financialStressByHouseholdId: financialStressByHouseholdId ?? new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>(),
            costOfLivingState: costOfLivingState,
            serviceQualityState: serviceQualityState,
            districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId ?? new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
            housingAutonomyPolicy: housingAutonomyPolicy ?? CreatePolicy(),
            anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
            cityPopulationAnchorCatalogRepository: anchorCatalogRepository ?? new FakeCityPopulationAnchorCatalogRepository(),
            commuteRoutingService: commuteRoutingService ?? new RecordingCommuteRoutingService(),
            activityEntries: activityEntries ?? new List<CityPopulationActivityWriteModel>(),
            occurredAtUtc: OccurredAtUtc,
            cancellationToken: CancellationToken.None);
    }

    private static CityHousingAutonomyPolicy CreatePolicy()
    {
        return new CityHousingAutonomyPolicy(
            householdEconomyPolicy: new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                householdCashflowPolicy: new CityHouseholdCashflowPolicy()));
    }

    private static (PersonEntity Resident, HouseholdId HouseholdId) FindStableFindHousingHousehold(
        CityHousingAutonomyPolicy policy)
    {
        HouseholdId opportunityHouseholdId = HouseholdId.From(CreateGuid(700_001));
        for (int seed = 1; seed <= 5_000; seed++)
        {
            HouseholdId householdId = HouseholdId.From(CreateGuid(710_000 + seed));
            CityAnchorId workplaceAnchorId = CityAnchorId.From(CreateGuid(720_000 + seed));
            PersonEntity resident = CreateResident(
                personId: CreateGuid(730_000 + seed),
                householdId: householdId,
                employmentStatus: EmploymentStatus.Employed,
                happiness: 100,
                health: 100,
                stress: 0,
                job: new Job(
                    workplaceId: WorkplaceId.From(CreateGuid(740_000 + seed)),
                    title: "Engineer",
                    workplaceAnchorId: workplaceAnchorId));

            IReadOnlyList<CityHousingAutonomyDecision> decisions = policy.Plan(
                households: new Dictionary<HouseholdId, HouseholdEntity>
                {
                    [householdId] = CreateHousehold(householdId, cashReserve: 20_000m),
                    [opportunityHouseholdId] = CreateHousehold(opportunityHouseholdId, cashReserve: 5_000m)
                },
                residents: [resident],
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Homeless,
                    [opportunityHouseholdId] = HousingStatus.Housed
                },
                financialStressStates: new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>(),
                commutePressureProfiles: null,
                districtUtilityConditionsByHouseholdId: null,
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                serviceQualityState: CreateServiceQualityState(housingSupportIndex: 3m));

            if (decisions.Count == 1 &&
                decisions[0].Type == CityHousingAutonomyDecisionType.FindHousing &&
                decisions[0].HouseholdId == householdId)
                return (resident, householdId);
        }

        throw new XunitException("Expected deterministic homeless household to produce a find-housing decision.");
    }

    private static Dictionary<PersonId, PersonEntity> CreateResidentsById(
        params PersonEntity[] residents)
    {
        return residents.ToDictionary(x => x.Id);
    }

    private static PersonEntity CreateResident(
        Guid personId,
        HouseholdId householdId,
        EmploymentStatus employmentStatus = EmploymentStatus.Unemployed,
        int happiness = 70,
        int health = 90,
        int stress = 20,
        Job? job = null)
    {
        return PersonEntity.CreatePerson(
            id: PersonId.From(personId),
            householdId: householdId,
            name: new PersonName("Alex", "Orlov"),
            sex: Sex.Male,
            lifeStatus: LifeStatus.Alive,
            maritalStatus: MaritalStatus.Single,
            spouseId: null,
            educationLevel: EducationLevel.UpperSecondary,
            educationInstitutionId: null,
            educationInstitutionAnchorId: null,
            employmentStatus: employmentStatus,
            happinessLevel: HappinessLevel.From(happiness),
            energyLevel: EnergyLevel.From(90),
            stressLevel: StressLevel.From(stress),
            socialNeedLevel: SocialNeedLevel.From(80),
            personality: Personality.Create(
                optimism: 90,
                discipline: 90,
                riskTolerance: 50,
                sociability: 80),
            birthDate: new DateOnly(2020, 5, 2),
            healthLevel: HealthLevel.From(health),
            weight: BodyWeight.FromKilograms(72m),
            job: job,
            currentDate: CurrentDate,
            illness: IllnessInfo.Healthy());
    }

    private static HouseholdEntity CreateHousehold(
        HouseholdId householdId,
        decimal cashReserve)
    {
        return HouseholdEntity.Create(
            id: householdId,
            size: HouseholdSize.From(1),
            createdAtUtc: CreatedAtUtc,
            cashReserve: Money.FromDecimal(cashReserve));
    }

    private static ClassicCityHouseholdPlacement CreateHousedPlacement(
        HouseholdId householdId,
        DistrictId districtId,
        ResidentialBuildingId residentialBuildingId)
    {
        return ClassicCityHouseholdPlacement.CreateHoused(
            householdId: householdId,
            cityId: TestCityId,
            districtId: districtId,
            residentialBuildingId: residentialBuildingId);
    }

    private static CityPopulationHouseholdFinancialStressState CreateFinancialStressState(
        HouseholdId householdId,
        decimal distressScore,
        int oldestOverdueAgeDays)
    {
        return CityPopulationHouseholdFinancialStressState.Create(
            cityId: TestCityId,
            householdId: householdId,
            overdueObligationCount: 3,
            overdueRentCount: 2,
            overdueUtilityCount: 1,
            arrearsObligationCount: 1,
            serviceCutoffCount: 1,
            evictionNoticeCount: 1,
            evictionEligibleCount: 1,
            oldestOverdueAgeDays: oldestOverdueAgeDays,
            totalOverdueAmount: 1_500m,
            distressScore: distressScore,
            lastEvaluatedAtUtc: EvaluatedAtUtc,
            updatedAtUtc: EvaluatedAtUtc);
    }

    private static CityPopulationServiceQualityState CreateServiceQualityState(
        decimal housingSupportIndex)
    {
        return CityPopulationServiceQualityState.Create(
            cityId: TestCityId,
            healthcareQualityIndex: 1m,
            educationQualityIndex: 1m,
            housingSupportIndex: housingSupportIndex,
            lastEvaluatedAtUtc: EvaluatedAtUtc,
            updatedAtUtc: EvaluatedAtUtc);
    }

    private static DistrictId CreateDistrictId(int seed)
    {
        return DistrictId.From(CreateGuid(800_000 + seed));
    }

    private static ResidentialBuildingId CreateResidentialBuildingId(int seed)
    {
        return ResidentialBuildingId.From(CreateGuid(900_000 + seed));
    }

    private static Guid CreateGuid(int seed)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
    }

    private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
    {
        public List<IReadOnlyCollection<CityPopulationCommuteRouteRequest>> PreloadRequests { get; } = [];
        public List<(ResidentialBuildingId? ResidentialBuildingId, CityAnchorId? DestinationAnchorId)> AnchorRequests { get; } = [];
        public CityPopulationCommuteContext DefaultAnchorContext { get; set; } = CityPopulationCommuteContext.Neutral;

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
            return Task.FromResult(DefaultAnchorContext);
        }

        public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            PersonEntity resident,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CityPopulationCommuteContext.Neutral);
        }

        public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            PersonEntity resident,
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
