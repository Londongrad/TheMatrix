using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityHouseholdIndependenceAutonomyPolicyTests
{
    [Fact]
    public void Plan_WhenReviewWindowDoesNotAdvance_ReturnsEmpty()
    {
        var policy = CreatePolicy();
        HouseholdId householdId = HouseholdId.From(Guid.Parse("10101010-2020-3030-4040-505050505050"));

        IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
            residents:
            [
                CreateResident(
                    personId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001"),
                    householdId: householdId,
                    birthDate: new DateOnly(2010, 5, 3))
            ],
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Housed
            },
            previousDate: new DateOnly(2048, 5, 3),
            currentDate: new DateOnly(2048, 5, 3));

        Assert.Empty(decisions);
    }

    [Fact]
    public void Plan_WhenHouseholdIsNotHoused_ReturnsEmpty()
    {
        var policy = CreatePolicy();
        HouseholdId householdId = HouseholdId.From(Guid.Parse("20202020-3030-4040-5050-606060606060"));
        PersonId motherId = PersonId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"));
        PersonId fatherId = PersonId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));

        IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
            residents:
            [
                CreateResident(
                    personId: motherId.Value,
                    householdId: householdId,
                    sex: Sex.Female,
                    birthDate: new DateOnly(2013, 5, 3),
                    maritalStatus: MaritalStatus.Married,
                    spouseId: fatherId),
                CreateResident(
                    personId: fatherId.Value,
                    householdId: householdId,
                    birthDate: new DateOnly(2011, 5, 3),
                    maritalStatus: MaritalStatus.Married,
                    spouseId: motherId),
                CreateResident(
                    personId: Guid.Parse("33333333-aaaa-bbbb-cccc-333333333333"),
                    householdId: householdId,
                    birthDate: new DateOnly(2024, 5, 3),
                    motherId: motherId,
                    fatherId: fatherId,
                    employmentStatus: EmploymentStatus.Employed)
            ],
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Homeless
            },
            previousDate: new DateOnly(2047, 11, 1),
            currentDate: new DateOnly(2048, 5, 3));

        Assert.Empty(decisions);
    }

    [Fact]
    public void Plan_WhenOnlyEligibleAdultHasChildInSameHousehold_ReturnsEmpty()
    {
        var policy = CreatePolicy();
        HouseholdId householdId = HouseholdId.From(Guid.Parse("30303030-4040-5050-6060-707070707070"));
        PersonId candidateId = PersonId.From(Guid.Parse("44444444-aaaa-bbbb-cccc-444444444444"));

        IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
            residents:
            [
                CreateResident(
                    personId: candidateId.Value,
                    householdId: householdId,
                    birthDate: new DateOnly(2023, 5, 3),
                    employmentStatus: EmploymentStatus.Employed,
                    maritalStatus: MaritalStatus.Single,
                    happiness: 20,
                    stress: 90),
                CreateResident(
                    personId: Guid.Parse("55555555-aaaa-bbbb-cccc-555555555555"),
                    householdId: householdId,
                    birthDate: new DateOnly(2048, 3, 1),
                    employmentStatus: EmploymentStatus.None,
                    motherId: candidateId,
                    sex: Sex.Female),
                CreateResident(
                    personId: Guid.Parse("66666666-aaaa-bbbb-cccc-666666666666"),
                    householdId: householdId,
                    birthDate: new DateOnly(2018, 5, 3),
                    maritalStatus: MaritalStatus.Married,
                    spouseId: PersonId.From(Guid.Parse("77777777-aaaa-bbbb-cccc-777777777777"))),
                CreateResident(
                    personId: Guid.Parse("77777777-aaaa-bbbb-cccc-777777777777"),
                    householdId: householdId,
                    sex: Sex.Female,
                    birthDate: new DateOnly(2019, 5, 3),
                    maritalStatus: MaritalStatus.Married,
                    spouseId: PersonId.From(Guid.Parse("66666666-aaaa-bbbb-cccc-666666666666")))
            ],
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Housed
            },
            previousDate: new DateOnly(2047, 11, 1),
            currentDate: new DateOnly(2048, 5, 3));

        Assert.Empty(decisions);
    }

    [Fact]
    public void Plan_WhenCrowdedAdultLivesWithParentsAndRollOccurs_ReturnsMoveOutDecision()
    {
        var policy = CreatePolicy();
        DateOnly previousDate = new(2047, 11, 1);
        DateOnly currentDate = new(2048, 5, 3);

        for (int seed = 1; seed <= 1_000; seed++)
        {
            HouseholdId householdId = HouseholdId.From(CreateGuid(100_000 + seed));
            PersonId motherId = PersonId.From(CreateGuid(110_000 + seed));
            PersonId fatherId = PersonId.From(CreateGuid(120_000 + seed));
            PersonId candidateId = PersonId.From(CreateGuid(130_000 + seed));

            Matrix.Population.Domain.Entities.Person[] residents =
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
                    personality: Personality.Create(optimism: 100, discipline: 100, riskTolerance: 50, sociability: 80)),
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
                previousDate: previousDate,
                currentDate: currentDate);

            CityHouseholdIndependenceAutonomyDecision? decision = decisions.SingleOrDefault();
            if (decision is not null)
            {
                Assert.Equal(candidateId, decision.ResidentId);
                Assert.Equal(householdId, decision.SourceHouseholdId);
                return;
            }
        }

        throw new XunitException("Expected at least one deterministic crowded household to produce a move-out decision.");
    }

    private static CityHouseholdIndependenceAutonomyPolicy CreatePolicy()
    {
        return new CityHouseholdIndependenceAutonomyPolicy(
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
    }

    private static Matrix.Population.Domain.Entities.Person CreateResident(
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
        return Person.CreatePerson(
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
            currentDate: new DateOnly(2048, 5, 3),
            illness: IllnessInfo.Healthy(),
            motherId: motherId,
            fatherId: fatherId);
    }

    private static Guid CreateGuid(int seed)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
    }
}
