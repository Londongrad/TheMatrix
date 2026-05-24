using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
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

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class ResidentTimeProgressionStepTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly HouseholdId TestHouseholdId = HouseholdId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly ResidentialBuildingId TestResidentialBuildingId = ResidentialBuildingId.From(
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly DateOnly PreviousDate = new(2048, 5, 1);
    private static readonly DateOnly CurrentDate = new(2048, 5, 2);
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApplyAsync_WhenResidentIsDead_ReturnsFalseAndDoesNotCallRouting()
    {
        PersonEntity resident = CreateResident(lifeStatus: LifeStatus.Deceased);
        var routingService = new RecordingCommuteRoutingService();

        bool changed = await ApplyAsync(
            resident: resident,
            householdsById: CreateHouseholdsMap(resident.HouseholdId),
            residentsByHouseholdId: CreateResidentsMap(resident),
            workplaceAnchors: [CreateAnchor(CreateAnchorId(1), CityAnchorType.Workplace)],
            schoolAnchors: [CreateAnchor(CreateAnchorId(2), CityAnchorType.School)],
            commuteRoutingService: routingService);

        Assert.False(changed);
        Assert.Equal(0, routingService.PreloadCallCount);
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
            workplaceAnchors: [CreateAnchor(CreateAnchorId(3), CityAnchorType.Workplace)],
            schoolAnchors: [CreateAnchor(CreateAnchorId(4), CityAnchorType.School)],
            commuteRoutingService: routingService);

        Assert.False(changed);
        Assert.Equal(0, routingService.PreloadCallCount);
        Assert.Empty(routingService.ResolvedAnchorIds);
    }

    [Fact]
    public async Task ApplyAsync_WhenHouseholdExists_RanksSchoolAndWorkplaceAnchors()
    {
        PersonEntity resident = CreateResident(birthDate: new DateOnly(2025, 5, 2));
        CityPopulationAnchorCatalogItem schoolA = CreateAnchor(CreateAnchorId(5), CityAnchorType.School);
        CityPopulationAnchorCatalogItem schoolB = CreateAnchor(CreateAnchorId(6), CityAnchorType.School);
        CityPopulationAnchorCatalogItem workplaceA = CreateAnchor(CreateAnchorId(7), CityAnchorType.Workplace);
        CityPopulationAnchorCatalogItem workplaceB = CreateAnchor(CreateAnchorId(8), CityAnchorType.Workplace);
        var routingService = new RecordingCommuteRoutingService();

        await ApplyAsync(
            resident: resident,
            householdsById: CreateHouseholdsMap(resident.HouseholdId),
            residentsByHouseholdId: CreateResidentsMap(resident),
            residentialBuildingByHouseholdId: CreateResidentialBuildingMap(resident.HouseholdId),
            workplaceAnchors: [workplaceA, workplaceB],
            schoolAnchors: [schoolA, schoolB],
            commuteRoutingService: routingService);

        Assert.Equal(2, routingService.PreloadCallCount);
        Assert.Equal(4, routingService.PreloadRequests.Count);
        Assert.All(
            routingService.PreloadRequests,
            request =>
            {
                Assert.Equal(TestResidentialBuildingId, request.ResidentialBuildingId);
                Assert.Equal(CityPopulationCommuteRoutingProfiles.Pedestrian, request.Profile);
            });
        Assert.Equal(
            [schoolA.CityAnchorId, schoolB.CityAnchorId, workplaceA.CityAnchorId, workplaceB.CityAnchorId],
            routingService.PreloadRequests.Select(request => request.DestinationAnchorId).ToArray());
        Assert.Equal(
            [schoolA.CityAnchorId, schoolB.CityAnchorId, workplaceA.CityAnchorId, workplaceB.CityAnchorId],
            routingService.ResolvedAnchorIds.ToArray());
    }

    [Fact]
    public async Task ApplyAsync_WhenResidentialBuildingIsMissing_DoesNotRouteAnchors()
    {
        PersonEntity resident = CreateResident(birthDate: new DateOnly(2025, 5, 2));
        var routingService = new RecordingCommuteRoutingService();

        await ApplyAsync(
            resident: resident,
            householdsById: CreateHouseholdsMap(resident.HouseholdId),
            residentsByHouseholdId: CreateResidentsMap(resident),
            workplaceAnchors: [CreateAnchor(CreateAnchorId(9), CityAnchorType.Workplace)],
            schoolAnchors: [CreateAnchor(CreateAnchorId(10), CityAnchorType.School)],
            commuteRoutingService: routingService);

        Assert.Equal(0, routingService.PreloadCallCount);
        Assert.Empty(routingService.PreloadRequests);
        Assert.Empty(routingService.ResolvedAnchorIds);
    }

    [Fact]
    public async Task ApplyAsync_WhenSeniorResidentIsEmployed_RetiresResidentAndReturnsTrue()
    {
        PersonEntity resident = CreateResident(
            birthDate: new DateOnly(1970, 5, 2),
            employmentStatus: EmploymentStatus.Employed,
            creationDate: new DateOnly(2030, 5, 2),
            job: new Job(
                workplaceId: WorkplaceId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                title: "Engineer",
                workplaceAnchorId: null));

        bool changed = await ApplyAsync(
            resident: resident,
            householdsById: CreateHouseholdsMap(resident.HouseholdId),
            residentsByHouseholdId: CreateResidentsMap(resident));

        Assert.True(changed);
        Assert.Equal(EmploymentStatus.Retired, resident.Employment.Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenSeniorResidentIsStudent_RetiresResidentAndReturnsTrue()
    {
        PersonEntity resident = CreateResident(
            birthDate: new DateOnly(1970, 5, 2),
            employmentStatus: EmploymentStatus.Student,
            creationDate: new DateOnly(2030, 5, 2));

        bool changed = await ApplyAsync(
            resident: resident,
            householdsById: CreateHouseholdsMap(resident.HouseholdId),
            residentsByHouseholdId: CreateResidentsMap(resident));

        Assert.True(changed);
        Assert.Equal(EmploymentStatus.Retired, resident.Employment.Status);
    }

    [Fact]
    public async Task ApplyAsync_WhenNonSeniorResidentHasNoPlacementOpportunities_ReturnsFalse()
    {
        PersonEntity resident = CreateResident(
            birthDate: new DateOnly(2018, 5, 2),
            employmentStatus: EmploymentStatus.Retired);
        EmploymentStatus initialStatus = resident.Employment.Status;
        EducationLevel initialEducationLevel = resident.EducationLevel;

        bool changed = await ApplyAsync(
            resident: resident,
            householdsById: CreateHouseholdsMap(resident.HouseholdId),
            residentsByHouseholdId: CreateResidentsMap(resident),
            housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
            {
                [resident.HouseholdId] = HousingStatus.Housed
            });

        Assert.False(changed);
        Assert.Equal(initialStatus, resident.Employment.Status);
        Assert.Equal(initialEducationLevel, resident.EducationLevel);
    }

    private static Task<bool> ApplyAsync(
        PersonEntity resident,
        IReadOnlyDictionary<HouseholdId, HouseholdEntity>? householdsById = null,
        IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>? residentsByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, HousingStatus>? housingByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, DistrictId?>? districtByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?>? residentialBuildingByHouseholdId = null,
        IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>? employerStressByWorkplaceId = null,
        CityPopulationCostOfLivingState? costOfLivingState = null,
        CityPopulationServiceQualityState? serviceQualityState = null,
        IDictionary<EducationLevel, List<CityEducationInstitutionBinding>>? institutionPools = null,
        IReadOnlyCollection<CityPopulationAnchorCatalogItem>? workplaceAnchors = null,
        IReadOnlyCollection<CityPopulationAnchorCatalogItem>? schoolAnchors = null,
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
            residentsByHouseholdId: residentsByHouseholdId ?? new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>(),
            previousDate: previousDate ?? PreviousDate,
            currentDate: currentDate ?? CurrentDate,
            housingByHouseholdId: housingByHouseholdId ?? new Dictionary<HouseholdId, HousingStatus>(),
            districtByHouseholdId: districtByHouseholdId ?? new Dictionary<HouseholdId, DistrictId?>(),
            residentialBuildingByHouseholdId: residentialBuildingByHouseholdId ?? new Dictionary<HouseholdId, ResidentialBuildingId?>(),
            employerStressByWorkplaceId: employerStressByWorkplaceId ?? new Dictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>(),
            costOfLivingState: costOfLivingState,
            serviceQualityState: serviceQualityState,
            educationAutonomyPolicy: new CityEducationAutonomyPolicy(anchorSelectionPolicy),
            employmentAutonomyPolicy: new CityEmploymentAutonomyPolicy(
                contentCatalog: new TestPopulationGenerationContentCatalog(),
                householdEconomyPolicy: CreateHouseholdEconomyPolicy(),
                anchorSelectionPolicy: anchorSelectionPolicy),
            institutionPools: institutionPools ?? new Dictionary<EducationLevel, List<CityEducationInstitutionBinding>>(),
            workplaceAnchors: workplaceAnchors ?? [],
            schoolAnchors: schoolAnchors ?? [],
            workplacePools: workplacePools ?? new Dictionary<string, List<Job>>(StringComparer.OrdinalIgnoreCase),
            commuteRoutingService: commuteRoutingService ?? new RecordingCommuteRoutingService(),
            cancellationToken: CancellationToken.None);
    }

    private static IReadOnlyDictionary<HouseholdId, HouseholdEntity> CreateHouseholdsMap(
        HouseholdId householdId)
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
            birthDate: birthDate ?? new DateOnly(1990, 5, 2),
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

        public void SetAnchorContext(
            CityAnchorId anchorId,
            CityPopulationCommuteContext context)
        {
            _anchorContexts[anchorId] = context;
        }

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

        public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
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
