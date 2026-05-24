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
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class ResidentIllnessProgressionStepTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly DistrictId TestDistrictId = DistrictId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly ResidentialBuildingId TestResidentialBuildingId =
        ResidentialBuildingId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly DateOnly PreviousDate = new(2048, 5, 1);
    private static readonly DateOnly CurrentDate = new(2048, 5, 2);
    private static readonly DateTimeOffset EffectiveAtUtc = new(2048, 5, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApplyAsync_WhenCurrentDateDoesNotAdvance_ReturnsFalseButStillResolvesHealthcareContext()
    {
        Person resident = CreatePerson(currentDate: CurrentDate);
        var routingService = new RecordingCommuteRoutingService();

        bool changed = await ApplyAsync(
            resident: resident,
            previousDate: CurrentDate,
            currentDate: CurrentDate,
            commuteRoutingService: routingService);

        Assert.False(changed);
        Assert.Equal(1, routingService.HealthcareResolveCallCount);
    }

    [Fact]
    public async Task ApplyAsync_WhenHospitalAnchorAndResidentialBuildingExist_ResolvesHealthcareCommuteWithSelectedAnchor()
    {
        Person resident = CreatePerson(currentDate: CurrentDate);
        CityAnchorId healthcareAnchorId = CityAnchorId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        var routingService = new RecordingCommuteRoutingService();

        await ApplyAsync(
            resident: resident,
            districtByHouseholdId: new Dictionary<HouseholdId, DistrictId?>
            {
                [resident.HouseholdId] = TestDistrictId
            },
            residentialBuildingByHouseholdId: new Dictionary<HouseholdId, ResidentialBuildingId?>
            {
                [resident.HouseholdId] = TestResidentialBuildingId
            },
            hospitalAnchors: [CreateHospitalAnchor(TestDistrictId, healthcareAnchorId)],
            commuteRoutingService: routingService);

        Assert.Equal(1, routingService.HealthcareResolveCallCount);
        Assert.Equal(TestCityId.Value, routingService.LastHealthcareCityId);
        Assert.Equal(TestResidentialBuildingId, routingService.LastHealthcareResidentialBuildingId);
        Assert.Equal(healthcareAnchorId, routingService.LastHealthcareAnchorId);
    }

    [Fact]
    public async Task ApplyAsync_WhenNoHospitalAnchorExists_ResolvesHealthcareCommuteWithNullAnchor()
    {
        Person resident = CreatePerson(currentDate: CurrentDate);
        var routingService = new RecordingCommuteRoutingService();

        await ApplyAsync(
            resident: resident,
            residentialBuildingByHouseholdId: new Dictionary<HouseholdId, ResidentialBuildingId?>
            {
                [resident.HouseholdId] = TestResidentialBuildingId
            },
            hospitalAnchors: [],
            commuteRoutingService: routingService);

        Assert.Equal(1, routingService.HealthcareResolveCallCount);
        Assert.Equal(TestResidentialBuildingId, routingService.LastHealthcareResidentialBuildingId);
        Assert.Null(routingService.LastHealthcareAnchorId);
    }

    [Fact]
    public async Task ApplyAsync_WhenResidentHasSevereInfectionAndLowSupport_AppliesIllnessBurden()
    {
        Person resident = CreatePerson(
            personId: Guid.Parse("c9b0f08a-8a88-4e88-9a6d-9c1efad0fa11"),
            currentDate: CurrentDate);
        resident.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Severe,
            currentDate: PreviousDate);
        resident.ChangeHealth(-70, CurrentDate);
        resident.ChangeEnergy(-70);
        resident.ChangeStress(70);
        IllnessSnapshot before = IllnessSnapshot.Capture(resident);
        var routingService = new RecordingCommuteRoutingService
        {
            HealthcareContext = CityPopulationCommuteContext.Blocked
        };

        bool changed = await ApplyAsync(
            resident: resident,
            housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
            {
                [resident.HouseholdId] = HousingStatus.Homeless
            },
            exposureSegments: [CreateAdverseSegment()],
            healthcarePressureProfile: CreateHealthcarePressureProfile(
                recoverySupportIndex: 0.45m,
                triagePressureIndex: 3m),
            commuteRoutingService: routingService);

        Assert.True(changed);
        Assert.True(resident.HasActiveIllness);
        Assert.Equal(IllnessSeverity.Severe, resident.CurrentIllnessSeverity);
        Assert.True(resident.Health.Value <= before.Health);
        Assert.True(resident.Happiness.Value <= before.Happiness);
        Assert.True(resident.Energy.Value <= before.Energy);
        Assert.True(resident.Stress.Value >= before.Stress);
    }

    [Fact]
    public async Task ApplyAsync_WhenAdverseExposureCanDiagnoseExposureIllness_DiagnosesExposureIllness()
    {
        Person resident = CreatePerson(
            personId: Guid.Parse("c9b0f08a-8a88-4e88-9a6d-000000000014"),
            birthDate: new DateOnly(1960, 5, 2),
            currentDate: CurrentDate,
            health: 20,
            energy: 70);

        bool changed = await ApplyAsync(
            resident: resident,
            previousDate: CurrentDate.AddDays(-7),
            housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
            {
                [resident.HouseholdId] = HousingStatus.Homeless
            },
            exposureSegments: [CreateAdverseSegment()]);

        Assert.True(changed);
        Assert.True(resident.HasActiveIllness);
        Assert.Equal(IllnessKind.Exposure, resident.CurrentIllnessKind);
    }

    [Fact]
    public async Task ApplyAsync_WhenResidentIsAlreadyDead_ReturnsFalse()
    {
        Person resident = CreatePerson(currentDate: CurrentDate);
        resident.Die(CurrentDate);
        IllnessSnapshot before = IllnessSnapshot.Capture(resident);

        bool changed = await ApplyAsync(
            resident: resident,
            exposureSegments: [CreateAdverseSegment()]);

        Assert.False(changed);
        Assert.Equal(before, IllnessSnapshot.Capture(resident));
        Assert.False(resident.IsAlive);
    }

    [Fact]
    public async Task ApplyAsync_WhenIllnessKillsMarriedResident_RegistersSpouseWidowhood()
    {
        var marriageDomainService = new MarriageDomainService();
        Guid householdId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        Person spouse = CreatePerson(
            personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            householdId: householdId,
            sex: Sex.Female,
            firstName: "Trinity",
            lastName: "Matrix",
            birthDate: new DateOnly(1960, 5, 2),
            currentDate: CurrentDate);
        Person resident = CreatePerson(
            personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            householdId: householdId,
            sex: Sex.Male,
            birthDate: new DateOnly(1960, 5, 2),
            currentDate: CurrentDate,
            health: 1);
        resident.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Severe,
            currentDate: PreviousDate);
        marriageDomainService.RegisterMarriage(
            person: resident,
            spouse: spouse,
            currentDate: CurrentDate);

        bool changed = await ApplyAsync(
            resident: resident,
            residentsById: new Dictionary<PersonId, Person>
            {
                [resident.Id] = resident,
                [spouse.Id] = spouse
            },
            housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
            {
                [resident.HouseholdId] = HousingStatus.Homeless
            },
            healthcarePressureProfile: CreateHealthcarePressureProfile(
                recoverySupportIndex: 0.45m,
                triagePressureIndex: 3m),
            commuteRoutingService: new RecordingCommuteRoutingService
            {
                HealthcareContext = CityPopulationCommuteContext.Blocked
            },
            marriageDomainService: marriageDomainService);

        Assert.True(changed);
        Assert.False(resident.IsAlive);
        Assert.Equal(MaritalStatus.Widowed, spouse.MaritalStatus);
        Assert.Null(spouse.SpouseId);
    }

    private static Task<bool> ApplyAsync(
        Person resident,
        IReadOnlyDictionary<PersonId, Person>? residentsById = null,
        IReadOnlyDictionary<HouseholdId, HousingStatus>? housingByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, DistrictId?>? districtByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?>? residentialBuildingByHouseholdId = null,
        IReadOnlyCollection<CityWeatherExposureSegment>? exposureSegments = null,
        CityPopulationLivingConditionsState? livingConditionsState = null,
        IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>? districtUtilityConditionsByDistrictId = null,
        CityPopulationEssentialsState? essentialsState = null,
        CityPopulationServiceQualityState? serviceQualityState = null,
        CityPopulationHealthcarePressureProfile? healthcarePressureProfile = null,
        IReadOnlyCollection<CityPopulationAnchorCatalogItem>? hospitalAnchors = null,
        RecordingCommuteRoutingService? commuteRoutingService = null,
        DateOnly? previousDate = null,
        DateOnly? currentDate = null,
        MarriageDomainService? marriageDomainService = null,
        CityIllnessAutonomyPolicy? illnessAutonomyPolicy = null,
        CityHealthcareAutonomyPolicy? healthcareAutonomyPolicy = null,
        CityPopulationAnchorSelectionPolicy? anchorSelectionPolicy = null,
        CityPopulationDistrictImpactPolicy? districtImpactPolicy = null,
        CityPopulationLivingConditionsPressurePolicy? livingConditionsPressurePolicy = null)
    {
        return ResidentIllnessProgressionStep.ApplyAsync(
            person: resident,
            cityId: TestCityId,
            residentsById: residentsById ?? new Dictionary<PersonId, Person>
            {
                [resident.Id] = resident
            },
            previousDate: previousDate ?? PreviousDate,
            currentDate: currentDate ?? CurrentDate,
            housingByHouseholdId: housingByHouseholdId ?? new Dictionary<HouseholdId, HousingStatus>(),
            districtByHouseholdId: districtByHouseholdId ?? new Dictionary<HouseholdId, DistrictId?>(),
            residentialBuildingByHouseholdId: residentialBuildingByHouseholdId ??
                                             new Dictionary<HouseholdId, ResidentialBuildingId?>(),
            exposureSegments: exposureSegments ?? [],
            livingConditionsState: livingConditionsState,
            districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId ??
                                                   new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
            essentialsState: essentialsState,
            serviceQualityState: serviceQualityState,
            healthcarePressureProfile: healthcarePressureProfile ?? CreateHealthcarePressureProfile(),
            marriageDomainService: marriageDomainService ?? new MarriageDomainService(),
            illnessAutonomyPolicy: illnessAutonomyPolicy ?? new CityIllnessAutonomyPolicy(),
            healthcareAutonomyPolicy: healthcareAutonomyPolicy ?? CreateHealthcarePolicy(),
            anchorSelectionPolicy: anchorSelectionPolicy ?? new CityPopulationAnchorSelectionPolicy(),
            hospitalAnchors: hospitalAnchors ?? [],
            districtImpactPolicy: districtImpactPolicy ?? new CityPopulationDistrictImpactPolicy(),
            livingConditionsPressurePolicy: livingConditionsPressurePolicy ??
                                            new CityPopulationLivingConditionsPressurePolicy(),
            commuteRoutingService: commuteRoutingService ?? new RecordingCommuteRoutingService(),
            cancellationToken: CancellationToken.None);
    }

    private static CityHealthcareAutonomyPolicy CreateHealthcarePolicy()
    {
        return new CityHealthcareAutonomyPolicy(
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
    }

    private static CityPopulationHealthcarePressureProfile CreateHealthcarePressureProfile(
        decimal recoverySupportIndex = 1m,
        decimal triagePressureIndex = 0m)
    {
        return new CityPopulationHealthcarePressureProfile(
            ActiveIllnessCount: 0,
            SevereIllnessCount: 0,
            MedicalLoadIndex: 0m,
            TriagePressureIndex: triagePressureIndex,
            RecoverySupportIndex: recoverySupportIndex);
    }

    private static CityPopulationAnchorCatalogItem CreateHospitalAnchor(
        DistrictId districtId,
        CityAnchorId anchorId)
    {
        return CityPopulationAnchorCatalogItem.Create(
            cityId: TestCityId,
            cityAnchorId: anchorId,
            districtId: districtId,
            accessRoadNodeId: RoadNodeId.From(Guid.NewGuid()),
            name: "Primary Care",
            type: CityAnchorType.Hospital,
            capacity: 100,
            positionX: 0m,
            positionY: 0m,
            createdAtUtc: EffectiveAtUtc);
    }

    private static CityWeatherExposureSegment CreateAdverseSegment()
    {
        return new CityWeatherExposureSegment(
            Kind: CityWeatherExposureKind.Adverse,
            Weather: new WeatherImpactProfile(
                Type: PopulationWeatherType.Heatwave,
                Severity: PopulationWeatherSeverity.Severe,
                PrecipitationKind: PopulationPrecipitationKind.None,
                TemperatureC: 40m,
                HumidityPercent: 45m,
                WindSpeedKph: 12m,
                CloudCoveragePercent: 35m,
                PressureHpa: 1012m),
            EffectStartedAtSimTimeUtc: EffectiveAtUtc.AddHours(-6),
            IntervalStartSimTimeUtc: EffectiveAtUtc.AddHours(-6),
            IntervalEndSimTimeUtc: EffectiveAtUtc);
    }

    private sealed record IllnessSnapshot(
        bool IsAlive,
        bool HasActiveIllness,
        IllnessKind? IllnessKind,
        IllnessSeverity? IllnessSeverity,
        int Health,
        int Energy,
        int Stress,
        int Happiness)
    {
        public static IllnessSnapshot Capture(Person person)
        {
            return new IllnessSnapshot(
                IsAlive: person.IsAlive,
                HasActiveIllness: person.HasActiveIllness,
                IllnessKind: person.CurrentIllnessKind,
                IllnessSeverity: person.CurrentIllnessSeverity,
                Health: person.Health.Value,
                Energy: person.Energy.Value,
                Stress: person.Stress.Value,
                Happiness: person.Happiness.Value);
        }
    }

    private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
    {
        public Guid? LastHealthcareCityId { get; private set; }
        public ResidentialBuildingId? LastHealthcareResidentialBuildingId { get; private set; }
        public CityAnchorId? LastHealthcareAnchorId { get; private set; }
        public int HealthcareResolveCallCount { get; private set; }
        public CityPopulationCommuteContext HealthcareContext { get; set; } = CityPopulationCommuteContext.Neutral;

        public Task PreloadAnchorCommutesAsync(
            Guid cityId,
            IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? destinationAnchorId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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
            HealthcareResolveCallCount++;
            LastHealthcareCityId = cityId;
            LastHealthcareResidentialBuildingId = residentialBuildingId;
            LastHealthcareAnchorId = healthcareAnchorId;

            return Task.FromResult(HealthcareContext);
        }
    }
}
