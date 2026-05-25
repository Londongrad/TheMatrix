using Matrix.BuildingBlocks.Domain.ValueObjects;
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
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class ResidentProgressionStepTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly HouseholdId TestHouseholdId = HouseholdId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateOnly PreviousDate = new(2048, 5, 1);
    private static readonly DateOnly CurrentDate = new(2048, 5, 2);
    private static readonly DateTimeOffset FromUtc = new(2048, 5, 2, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ToUtc = new(2048, 5, 2, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApplyAsync_WhenNoProgressionIsRequiredAndNoExposureExists_ReturnsFalse()
    {
        PersonEntity resident = CreateResident();
        var routingService = new RecordingCommuteRoutingService();
        ResidentSnapshot before = ResidentSnapshot.Capture(resident);

        bool changed = await ApplyAsync(
            resident: resident,
            requiresDateProgression: false,
            requiresNeedsProgression: false,
            exposureSegments: [],
            commuteRoutingService: routingService);

        Assert.False(changed);
        Assert.Equal(before, ResidentSnapshot.Capture(resident));
        Assert.Equal(0, routingService.TotalCallCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenOnlyNeedsProgressionIsRequired_AppliesNeedsProgression()
    {
        PersonEntity resident = CreateResident();
        var policy = new PersonNeedsProgressionPolicy();
        PersonNeedsProgressionEffect expectedEffect = policy.Calculate(
            person: resident,
            fromSimTimeUtc: FromUtc,
            toSimTimeUtc: ToUtc,
            utcOffsetMinutes: 0);
        ResidentSnapshot before = ResidentSnapshot.Capture(resident);

        bool changed = await ApplyAsync(
            resident: resident,
            requiresDateProgression: false,
            requiresNeedsProgression: true,
            exposureSegments: [],
            personNeedsProgressionPolicy: policy);

        Assert.True(expectedEffect.HasAnyEffect);
        Assert.True(changed);
        Assert.Equal(before.Energy + expectedEffect.EnergyDelta, resident.Energy.Value);
        Assert.Equal(before.Stress + expectedEffect.StressDelta, resident.Stress.Value);
        Assert.Equal(before.SocialNeed + expectedEffect.SocialNeedDelta, resident.SocialNeed.Value);
        Assert.Equal(before.Health + expectedEffect.HealthDelta, resident.Health.Value);
        Assert.Equal(before.Happiness + expectedEffect.HappinessDelta, resident.Happiness.Value);
    }

    [Fact]
    public async Task ApplyAsync_WhenExposureSegmentsExist_AppliesWeatherExposureWithoutDateProgression()
    {
        DateOnly currentDate = new(2048, 7, 10);
        PersonEntity resident = CreateResident(
            birthDate: new DateOnly(1960, 7, 10),
            currentDate: currentDate);
        CityWeatherExposureSegment segment = CreateAdverseWeatherSegment(currentDate);
        var policy = CreateWeatherExposurePolicy();
        PersonWeatherImpact expectedImpact = policy.Calculate(
            person: resident,
            currentDate: currentDate,
            segment: segment,
            environment: null);
        ResidentSnapshot before = ResidentSnapshot.Capture(resident);

        bool changed = await ApplyAsync(
            resident: resident,
            currentDate: currentDate,
            requiresDateProgression: false,
            requiresNeedsProgression: false,
            exposureSegments: [segment],
            weatherExposurePolicy: policy);

        Assert.True(expectedImpact.HasEffect);
        Assert.True(changed);
        Assert.Equal(before.Health + expectedImpact.HealthDelta, resident.Health.Value);
        Assert.Equal(before.Happiness + expectedImpact.HappinessDelta, resident.Happiness.Value);
    }

    [Fact]
    public async Task ApplyAsync_WhenDateProgressionRetiresSeniorResident_ReturnsTrue()
    {
        PersonEntity resident = CreateResident(
            birthDate: new DateOnly(1970, 5, 2),
            currentDate: new DateOnly(2030, 5, 2),
            employmentStatus: EmploymentStatus.Employed,
            job: new Job(
                workplaceId: WorkplaceId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                title: "Engineer",
                workplaceAnchorId: null));

        bool changed = await ApplyAsync(
            resident: resident,
            requiresDateProgression: true,
            requiresNeedsProgression: false,
            exposureSegments: [],
            householdsById: CreateHouseholdsById(resident.HouseholdId),
            residentsByHouseholdId: CreateResidentsByHouseholdId(resident),
            housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
            {
                [resident.HouseholdId] = HousingStatus.Housed
            });

        Assert.True(changed);
        Assert.Equal(EmploymentStatus.Retired, resident.Employment.Status);
    }

    private static Task<bool> ApplyAsync(
        PersonEntity resident,
        DateOnly? previousDate = null,
        DateOnly? currentDate = null,
        bool requiresDateProgression = false,
        bool requiresNeedsProgression = false,
        CityPopulationEnvironment? environment = null,
        IReadOnlyCollection<CityWeatherExposureSegment>? exposureSegments = null,
        IReadOnlyDictionary<PersonId, PersonEntity>? residentsById = null,
        IReadOnlyDictionary<HouseholdId, HouseholdEntity>? householdsById = null,
        IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>? residentsByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, HousingStatus>? housingByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, DistrictId?>? districtByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?>? residentialBuildingByHouseholdId = null,
        IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>? employerStressByWorkplaceId = null,
        IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>? financialStressByHouseholdId = null,
        CityPopulationCostOfLivingState? costOfLivingState = null,
        CityPopulationEssentialsState? essentialsState = null,
        CityPopulationLivingConditionsState? livingConditionsState = null,
        IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>? districtUtilityConditionsByDistrictId = null,
        CityPopulationServiceQualityState? serviceQualityState = null,
        CityPopulationHealthcarePressureProfile? healthcarePressureProfile = null,
        PersonNeedsProgressionPolicy? personNeedsProgressionPolicy = null,
        CityPopulationWeatherExposurePolicy? weatherExposurePolicy = null,
        RecordingCommuteRoutingService? commuteRoutingService = null)
    {
        var anchorSelectionPolicy = new CityPopulationAnchorSelectionPolicy();
        var householdEconomyPolicy = new CityHouseholdEconomyPolicy(
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
            householdCashflowPolicy: new CityHouseholdCashflowPolicy());

        return ResidentProgressionStep.ApplyAsync(
            person: resident,
            cityId: TestCityId,
            residentsById: residentsById ?? new Dictionary<PersonId, PersonEntity>
            {
                [resident.Id] = resident
            },
            householdsById: householdsById ?? new Dictionary<HouseholdId, HouseholdEntity>(),
            residentsByHouseholdId: residentsByHouseholdId ?? new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>(),
            previousDate: previousDate ?? PreviousDate,
            fromSimTimeUtc: FromUtc,
            toSimTimeUtc: ToUtc,
            currentDate: currentDate ?? CurrentDate,
            requiresDateProgression: requiresDateProgression,
            requiresNeedsProgression: requiresNeedsProgression,
            environment: environment,
            exposureSegments: exposureSegments ?? [],
            housingByHouseholdId: housingByHouseholdId ?? new Dictionary<HouseholdId, HousingStatus>(),
            districtByHouseholdId: districtByHouseholdId ?? new Dictionary<HouseholdId, DistrictId?>(),
            residentialBuildingByHouseholdId: residentialBuildingByHouseholdId ??
                                             new Dictionary<HouseholdId, ResidentialBuildingId?>(),
            employerStressByWorkplaceId: employerStressByWorkplaceId ??
                                         new Dictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>(),
            financialStressByHouseholdId: financialStressByHouseholdId ??
                                          new Dictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>(),
            costOfLivingState: costOfLivingState,
            essentialsState: essentialsState,
            livingConditionsState: livingConditionsState,
            districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId ??
                                                   new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
            districtImpactPolicy: new CityPopulationDistrictImpactPolicy(),
            serviceQualityState: serviceQualityState,
            healthcarePressureProfile: healthcarePressureProfile ?? CreateHealthcarePressureProfile(),
            marriageDomainService: new MarriageDomainService(),
            educationAutonomyPolicy: new CityEducationAutonomyPolicy(anchorSelectionPolicy),
            employmentAutonomyPolicy: new CityEmploymentAutonomyPolicy(
                contentCatalog: new TestPopulationGenerationContentCatalog(),
                householdEconomyPolicy: householdEconomyPolicy,
                anchorSelectionPolicy: anchorSelectionPolicy),
            householdPressurePolicy: new CityHouseholdPressurePolicy(),
            illnessAutonomyPolicy: new CityIllnessAutonomyPolicy(),
            healthcareAutonomyPolicy: new CityHealthcareAutonomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy()),
            anchorSelectionPolicy: anchorSelectionPolicy,
            hospitalAnchors: [],
            livingConditionsPressurePolicy: new CityPopulationLivingConditionsPressurePolicy(),
            institutionPools: new Dictionary<EducationLevel, List<CityEducationInstitutionBinding>>(),
            workplaceAnchors: [],
            schoolAnchors: [],
            workplacePools: new Dictionary<string, List<Job>>(StringComparer.OrdinalIgnoreCase),
            personNeedsProgressionPolicy: personNeedsProgressionPolicy ?? new PersonNeedsProgressionPolicy(),
            weatherExposurePolicy: weatherExposurePolicy ?? CreateWeatherExposurePolicy(),
            commuteRoutingService: commuteRoutingService ?? new RecordingCommuteRoutingService(),
            cancellationToken: CancellationToken.None);
    }

    private static PersonEntity CreateResident(
        DateOnly? birthDate = null,
        DateOnly? currentDate = null,
        EmploymentStatus employmentStatus = EmploymentStatus.Unemployed,
        Job? job = null)
    {
        return CreatePerson(
            personId: Guid.NewGuid(),
            householdId: TestHouseholdId.Value,
            birthDate: birthDate ?? new DateOnly(1990, 5, 2),
            currentDate: currentDate ?? CurrentDate,
            employmentStatus: employmentStatus,
            job: job);
    }

    private static IReadOnlyDictionary<HouseholdId, HouseholdEntity> CreateHouseholdsById(
        HouseholdId householdId)
    {
        return new Dictionary<HouseholdId, HouseholdEntity>
        {
            [householdId] = HouseholdEntity.Create(
                id: householdId,
                size: HouseholdSize.From(1),
                createdAtUtc: CreatedAtUtc,
                cashReserve: Money.FromDecimal(100m))
        };
    }

    private static IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> CreateResidentsByHouseholdId(
        params PersonEntity[] residents)
    {
        return new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>
        {
            [residents[0].HouseholdId] = residents
        };
    }

    private static CityWeatherExposureSegment CreateAdverseWeatherSegment(
        DateOnly currentDate)
    {
        var dayStartUtc = new DateTimeOffset(
            currentDate.Year,
            currentDate.Month,
            currentDate.Day,
            0,
            0,
            0,
            TimeSpan.Zero);

        return new CityWeatherExposureSegment(
            Kind: CityWeatherExposureKind.Adverse,
            Weather: new WeatherImpactProfile(
                Type: PopulationWeatherType.Heatwave,
                Severity: PopulationWeatherSeverity.Extreme,
                PrecipitationKind: PopulationPrecipitationKind.None,
                TemperatureC: 39m,
                HumidityPercent: 45m,
                WindSpeedKph: 12m,
                CloudCoveragePercent: 35m,
                PressureHpa: 1012m),
            EffectStartedAtSimTimeUtc: dayStartUtc,
            IntervalStartSimTimeUtc: dayStartUtc,
            IntervalEndSimTimeUtc: dayStartUtc.AddHours(6));
    }

    private static CityPopulationWeatherExposurePolicy CreateWeatherExposurePolicy()
    {
        return new CityPopulationWeatherExposurePolicy(
            climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
    }

    private static CityPopulationHealthcarePressureProfile CreateHealthcarePressureProfile()
    {
        return new CityPopulationHealthcarePressureProfile(
            ActiveIllnessCount: 0,
            SevereIllnessCount: 0,
            MedicalLoadIndex: 0m,
            TriagePressureIndex: 0m,
            RecoverySupportIndex: 1m);
    }

    private sealed record ResidentSnapshot(
        int Health,
        int Happiness,
        int Energy,
        int Stress,
        int SocialNeed,
        EmploymentStatus EmploymentStatus)
    {
        public static ResidentSnapshot Capture(PersonEntity resident)
        {
            return new ResidentSnapshot(
                Health: resident.Health.Value,
                Happiness: resident.Happiness.Value,
                Energy: resident.Energy.Value,
                Stress: resident.Stress.Value,
                SocialNeed: resident.SocialNeed.Value,
                EmploymentStatus: resident.Employment.Status);
        }
    }

    private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
    {
        public int PreloadCallCount { get; private set; }
        public int AnchorCallCount { get; private set; }
        public int EmploymentCallCount { get; private set; }
        public int EducationCallCount { get; private set; }
        public int HealthcareCallCount { get; private set; }
        public int TotalCallCount =>
            PreloadCallCount + AnchorCallCount + EmploymentCallCount + EducationCallCount + HealthcareCallCount;

        public Task PreloadAnchorCommutesAsync(
            Guid cityId,
            IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
            CancellationToken cancellationToken)
        {
            PreloadCallCount++;
            return Task.CompletedTask;
        }

        public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? destinationAnchorId,
            CancellationToken cancellationToken)
        {
            AnchorCallCount++;
            return Task.FromResult(CityPopulationCommuteContext.Neutral);
        }

        public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            PersonEntity resident,
            CancellationToken cancellationToken)
        {
            EmploymentCallCount++;
            return Task.FromResult(CityPopulationCommuteContext.Neutral);
        }

        public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            PersonEntity resident,
            CancellationToken cancellationToken)
        {
            EducationCallCount++;
            return Task.FromResult(CityPopulationCommuteContext.Neutral);
        }

        public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? healthcareAnchorId,
            CancellationToken cancellationToken)
        {
            HealthcareCallCount++;
            return Task.FromResult(CityPopulationCommuteContext.Neutral);
        }
    }

    private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
    {
        public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
        public IReadOnlyList<string> FemaleFirstNames => ["Anna"];
        public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames => [];
        public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
        [
            new PopulationProfessionCatalogItem("Engineer", 1)
        ];
    }
}
