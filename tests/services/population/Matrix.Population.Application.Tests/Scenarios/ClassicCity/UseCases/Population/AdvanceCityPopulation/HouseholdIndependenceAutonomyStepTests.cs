using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
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

public sealed class HouseholdIndependenceAutonomyStepTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly DateOnly PreviousDate = new(2047, 11, 1);
    private static readonly DateOnly CurrentDate = new(2048, 5, 3);
    private static readonly DateTimeOffset OccurredAtUtc = new(2048, 5, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApplyAsync_WhenNoPlacementsExist_ReturnsZeroAndDoesNotPlanMoves()
    {
        var resident = CreateResident(
            personId: CreateGuid(1),
            householdId: HouseholdId.From(CreateGuid(101)),
            birthDate: new DateOnly(2023, 5, 3));
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult = []
        };
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
            cityId: TestCityId,
            residentsById: CreateResidentsById(resident),
            previousDate: PreviousDate,
            currentDate: CurrentDate,
            householdWriteRepository: householdWriteRepository,
            householdIndependenceAutonomyPolicy: CreatePolicy(),
            activityEntries: activityEntries,
            occurredAtUtc: OccurredAtUtc,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, affected);
        Assert.Equal(TestCityId, householdWriteRepository.RequestedCityId);
        Assert.Empty(activityEntries);
        Assert.Empty(householdWriteRepository.UpdatedHouseholds);
        Assert.Empty(householdWriteRepository.AddedHouseholds);
    }

    [Fact]
    public async Task ApplyAsync_WhenPolicyPlansNoMoves_ReturnsZero()
    {
        HouseholdId householdId = HouseholdId.From(CreateGuid(201));
        var resident = CreateResident(
            personId: CreateGuid(2),
            householdId: householdId,
            birthDate: new DateOnly(2023, 5, 3));
        var placement = CreateHousedPlacement(householdId);
        var householdWriteRepository = new FakeHouseholdWriteRepository
        {
            PlacementsByCityResult = [placement]
        };
        householdWriteRepository.PlacementsByHouseholdId[householdId] = placement;
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
            cityId: TestCityId,
            residentsById: CreateResidentsById(resident),
            previousDate: CurrentDate,
            currentDate: CurrentDate,
            householdWriteRepository: householdWriteRepository,
            householdIndependenceAutonomyPolicy: CreatePolicy(),
            activityEntries: activityEntries,
            occurredAtUtc: OccurredAtUtc,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, affected);
        Assert.Equal(householdId, resident.HouseholdId);
        Assert.Empty(activityEntries);
        Assert.Empty(householdWriteRepository.UpdatedHouseholds);
        Assert.Empty(householdWriteRepository.AddedHouseholds);
    }

    [Fact]
    public async Task ApplyAsync_WhenMoveOutDecisionIsPlanned_MovesResidentCreatesIndependentHouseholdAndWritesActivity()
    {
        var policy = CreatePolicy();
        (PersonEntity[] residents, PersonEntity candidate, HouseholdId householdId) = FindStableMoveOutHousehold(policy);
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        ConfigureHousedHousehold(
            repository: householdWriteRepository,
            householdId: householdId,
            residentsCount: residents.Length,
            cashReserve: 10_000m);
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
            cityId: TestCityId,
            residentsById: CreateResidentsById(residents),
            previousDate: PreviousDate,
            currentDate: CurrentDate,
            householdWriteRepository: householdWriteRepository,
            householdIndependenceAutonomyPolicy: policy,
            activityEntries: activityEntries,
            occurredAtUtc: OccurredAtUtc,
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, affected);
        Assert.NotEqual(householdId, candidate.HouseholdId);
        HouseholdEntity updatedSourceHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
        Assert.Equal(householdId, updatedSourceHousehold.Id);
        Assert.Equal(residents.Length - 1, updatedSourceHousehold.Size.Value);
        (HouseholdEntity newHousehold, ClassicCityHouseholdPlacement newPlacement) = Assert.Single(householdWriteRepository.AddedHouseholds);
        Assert.Equal(candidate.HouseholdId, newHousehold.Id);
        Assert.Equal(1, newHousehold.Size.Value);
        Assert.Equal(OccurredAtUtc, newHousehold.CreatedAtUtc);
        Assert.Equal(Money.FromDecimal(3_200m), newHousehold.CashReserve);
        Assert.Equal(candidate.HouseholdId, newPlacement.HouseholdId);
        Assert.Equal(TestCityId, newPlacement.CityId);
        Assert.Equal(HousingStatus.Homeless, newPlacement.HousingStatus);

        CityPopulationActivityWriteModel activity = Assert.Single(activityEntries);
        Assert.Equal(CityPopulationActivityEventType.ResidentFormedIndependentHousehold, activity.EventType);
        Assert.Equal(CityPopulationActivitySource.Autonomy, activity.Source);
        Assert.Equal(CityPopulationActivitySeverity.Success, activity.Severity);
        Assert.Equal(TestCityId.Value, activity.CityId);
        Assert.Equal(CurrentDate, activity.CurrentDate);
        Assert.Equal(OccurredAtUtc, activity.OccurredAtUtc);
        Assert.Equal(candidate.Id.Value, activity.PrimaryResidentId);
    }

    [Fact]
    public async Task ApplyAsync_WhenSourceHouseholdHasOneResident_SkipsMove()
    {
        var policy = CreatePolicy();
        (PersonEntity[] residents, PersonEntity candidate, HouseholdId householdId) = FindStableMoveOutHousehold(policy);
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        ConfigureHousedHousehold(
            repository: householdWriteRepository,
            householdId: householdId,
            residentsCount: 1,
            cashReserve: 10_000m);
        List<CityPopulationActivityWriteModel> activityEntries = [];

        int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
            cityId: TestCityId,
            residentsById: CreateResidentsById(residents),
            previousDate: PreviousDate,
            currentDate: CurrentDate,
            householdWriteRepository: householdWriteRepository,
            householdIndependenceAutonomyPolicy: policy,
            activityEntries: activityEntries,
            occurredAtUtc: OccurredAtUtc,
            cancellationToken: CancellationToken.None);

        Assert.Equal(0, affected);
        Assert.Equal(householdId, candidate.HouseholdId);
        Assert.Empty(activityEntries);
        Assert.Empty(householdWriteRepository.UpdatedHouseholds);
        Assert.Empty(householdWriteRepository.AddedHouseholds);
    }

    private static CityHouseholdIndependenceAutonomyPolicy CreatePolicy()
    {
        return new CityHouseholdIndependenceAutonomyPolicy(
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
    }

    private static (PersonEntity[] Residents, PersonEntity Candidate, HouseholdId HouseholdId) FindStableMoveOutHousehold(
        CityHouseholdIndependenceAutonomyPolicy policy)
    {
        for (int seed = 1; seed <= 1_000; seed++)
        {
            HouseholdId householdId = HouseholdId.From(CreateGuid(100_000 + seed));
            PersonId motherId = PersonId.From(CreateGuid(110_000 + seed));
            PersonId fatherId = PersonId.From(CreateGuid(120_000 + seed));
            PersonId candidateId = PersonId.From(CreateGuid(130_000 + seed));
            PersonEntity[] residents =
            [
                CreateResident(
                    personId: motherId.Value,
                    householdId: householdId,
                    sex: Sex.Female,
                    birthDate: new DateOnly(2010, 5, 3),
                    maritalStatus: MaritalStatus.Married,
                    spouseId: fatherId,
                    happiness: 20,
                    stress: 95),
                CreateResident(
                    personId: fatherId.Value,
                    householdId: householdId,
                    birthDate: new DateOnly(2008, 5, 3),
                    maritalStatus: MaritalStatus.Married,
                    spouseId: motherId,
                    happiness: 20,
                    stress: 95),
                CreateResident(
                    personId: candidateId.Value,
                    householdId: householdId,
                    birthDate: new DateOnly(2023, 5, 3),
                    maritalStatus: MaritalStatus.Single,
                    employmentStatus: EmploymentStatus.Employed,
                    happiness: 0,
                    health: 100,
                    stress: 100,
                    motherId: motherId,
                    fatherId: fatherId,
                    personality: Personality.Create(
                        optimism: 100,
                        discipline: 100,
                        riskTolerance: 50,
                        sociability: 80)),
                CreateResident(
                    personId: CreateGuid(140_000 + seed),
                    householdId: householdId,
                    birthDate: new DateOnly(2037, 5, 3),
                    employmentStatus: EmploymentStatus.None,
                    motherId: motherId,
                    fatherId: fatherId,
                    happiness: 30,
                    stress: 80),
                CreateResident(
                    personId: CreateGuid(150_000 + seed),
                    householdId: householdId,
                    birthDate: new DateOnly(2040, 5, 3),
                    employmentStatus: EmploymentStatus.None,
                    motherId: motherId,
                    fatherId: fatherId,
                    happiness: 30,
                    stress: 80)
            ];

            IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
                residents: residents,
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Housed
                },
                previousDate: PreviousDate,
                currentDate: CurrentDate);

            CityHouseholdIndependenceAutonomyDecision? decision = decisions.SingleOrDefault();
            if (decision is null)
                continue;

            PersonEntity candidate = residents.Single(x => x.Id == decision.ResidentId);
            return (residents, candidate, householdId);
        }

        throw new XunitException("Expected deterministic crowded household to produce a move-out decision.");
    }

    private static Dictionary<PersonId, PersonEntity> CreateResidentsById(
        params PersonEntity[] residents)
    {
        return residents.ToDictionary(x => x.Id);
    }

    private static PersonEntity CreateResident(
        Guid personId,
        HouseholdId householdId,
        DateOnly birthDate,
        Sex sex = Sex.Male,
        MaritalStatus maritalStatus = MaritalStatus.Single,
        PersonId? spouseId = null,
        EmploymentStatus employmentStatus = EmploymentStatus.Unemployed,
        int happiness = 50,
        int health = 80,
        int stress = 25,
        PersonId? motherId = null,
        PersonId? fatherId = null,
        Personality? personality = null)
    {
        return PersonEntity.CreatePerson(
            id: PersonId.From(personId),
            householdId: householdId,
            name: new PersonName("Alex", "Petrov"),
            sex: sex,
            lifeStatus: LifeStatus.Alive,
            maritalStatus: maritalStatus,
            spouseId: spouseId,
            educationLevel: EducationLevel.UpperSecondary,
            educationInstitutionId: null,
            educationInstitutionAnchorId: null,
            employmentStatus: employmentStatus,
            happinessLevel: HappinessLevel.From(happiness),
            energyLevel: EnergyLevel.From(80),
            stressLevel: StressLevel.From(stress),
            socialNeedLevel: SocialNeedLevel.From(40),
            personality: personality ?? Personality.Neutral(),
            birthDate: birthDate,
            healthLevel: HealthLevel.From(health),
            weight: BodyWeight.FromKilograms(70m),
            job: employmentStatus == EmploymentStatus.Employed
                ? new Job(
                    workplaceId: WorkplaceId.From(Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc")),
                    title: "Engineer",
                    workplaceAnchorId: null)
                : null,
            currentDate: CurrentDate,
            illness: IllnessInfo.Healthy(),
            motherId: motherId,
            fatherId: fatherId);
    }

    private static void ConfigureHousedHousehold(
        FakeHouseholdWriteRepository repository,
        HouseholdId householdId,
        int residentsCount,
        decimal cashReserve)
    {
        ClassicCityHouseholdPlacement placement = CreateHousedPlacement(householdId);
        repository.PlacementsByCityResult = [placement];
        repository.HouseholdsById[householdId] = CreateHousehold(
            householdId: householdId,
            size: residentsCount,
            cashReserve: cashReserve);
        repository.PlacementsByHouseholdId[householdId] = placement;
        repository.ResidentCountByHouseholdId[householdId] = residentsCount;
    }

    private static HouseholdEntity CreateHousehold(
        HouseholdId householdId,
        int size,
        decimal cashReserve)
    {
        return HouseholdEntity.Create(
            id: householdId,
            size: HouseholdSize.From(size),
            createdAtUtc: CreatedAtUtc,
            cashReserve: Money.FromDecimal(cashReserve));
    }

    private static ClassicCityHouseholdPlacement CreateHousedPlacement(
        HouseholdId householdId)
    {
        return ClassicCityHouseholdPlacement.CreateHoused(
            householdId: householdId,
            cityId: TestCityId,
            districtId: DistrictId.From(CreateGuid(Math.Abs(householdId.Value.GetHashCode()) % 900_000 + 200_000)),
            residentialBuildingId: ResidentialBuildingId.From(CreateGuid(Math.Abs(householdId.Value.GetHashCode()) % 900_000 + 300_000)));
    }

    private static Guid CreateGuid(int seed)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
    }
}
