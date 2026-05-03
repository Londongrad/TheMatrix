using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityBirthAutonomyPolicyTests
{
    [Fact]
    public void Constructor_WhenFemaleNameCatalogIsEmpty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CityBirthAutonomyPolicy(
                contentCatalog: new TestPopulationGenerationContentCatalog(
                    maleFirstNames: ["Ivan"],
                    femaleFirstNames: []),
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy()));
    }

    [Fact]
    public void Plan_WhenMotherRecentlyGaveBirth_ReturnsEmpty()
    {
        var policy = CreatePolicy();
        DateOnly currentDate = new(2048, 5, 1);
        PersonId motherId = PersonId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"));
        PersonId fatherId = PersonId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));
        HouseholdId householdId = HouseholdId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-333333333333"));
        Person mother = CreateParent(
            personId: motherId,
            spouseId: fatherId,
            householdId: householdId,
            sex: Sex.Female,
            birthDate: new DateOnly(2020, 5, 1),
            lastChildbirthDate: new DateOnly(2047, 9, 15));
        Person father = CreateParent(
            personId: fatherId,
            spouseId: motherId,
            householdId: householdId,
            sex: Sex.Male,
            birthDate: new DateOnly(2018, 5, 1));

        IReadOnlyList<CityBirthAutonomyDecision> decisions = policy.Plan(
            residents: [mother, father],
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Housed
            },
            previousDate: new DateOnly(2047, 11, 1),
            currentDate: currentDate);

        Assert.Empty(decisions);
    }

    [Fact]
    public void Plan_WhenStableMarriedCoupleRollBirth_ReturnsDecisionWithNewbornProfile()
    {
        var policy = CreatePolicy();
        DateOnly previousDate = new(2047, 11, 1);
        DateOnly currentDate = new(2048, 5, 1);

        for (int seed = 1; seed <= 2_000; seed++)
        {
            PersonId motherId = PersonId.From(CreateGuid(70_000 + seed));
            PersonId fatherId = PersonId.From(CreateGuid(80_000 + seed));
            HouseholdId householdId = HouseholdId.From(CreateGuid(90_000 + seed));
            Person mother = CreateParent(
                personId: motherId,
                spouseId: fatherId,
                householdId: householdId,
                sex: Sex.Female,
                birthDate: new DateOnly(2020, 5, 1),
                happiness: 100,
                health: 100,
                stress: 0,
                socialNeed: 100,
                sociability: 100);
            Person father = CreateParent(
                personId: fatherId,
                spouseId: motherId,
                householdId: householdId,
                sex: Sex.Male,
                birthDate: new DateOnly(2018, 5, 1),
                happiness: 100,
                health: 100,
                stress: 0,
                socialNeed: 100,
                sociability: 100);
            IReadOnlyList<CityBirthAutonomyDecision> decisions = policy.Plan(
                residents: [mother, father],
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Housed
                },
                previousDate: previousDate,
                currentDate: currentDate);

            if (decisions.Count == 1)
            {
                CityBirthAutonomyDecision decision = decisions[0];
                Assert.Equal(mother.Id, decision.MotherId);
                Assert.Equal(father.Id, decision.FatherId);
                Assert.NotEqual(Guid.Empty, decision.Newborn.PersonId.Value);
                Assert.Equal("Petrov", decision.Newborn.Name.LastName);
                Assert.Contains(decision.Newborn.Name.FirstName, new[] { "Anna", "Mikhail" });
                Assert.InRange(decision.Newborn.Health.Value, 75, 100);
                Assert.InRange(decision.Newborn.Weight.Kilograms, 3.0m, 4.5m);
                return;
            }
        }

        throw new XunitException("Expected at least one deterministic stable couple to schedule childbirth.");
    }

    [Fact]
    public void Plan_WhenHouseholdIsAlreadyAtMaximumSize_ReturnsEmpty()
    {
        var policy = CreatePolicy();
        DateOnly currentDate = new(2048, 5, 1);
        PersonId motherId = PersonId.From(Guid.Parse("44444444-aaaa-bbbb-cccc-444444444444"));
        PersonId fatherId = PersonId.From(Guid.Parse("55555555-aaaa-bbbb-cccc-555555555555"));
        HouseholdId householdId = HouseholdId.From(Guid.Parse("66666666-aaaa-bbbb-cccc-666666666666"));
        var residents = new List<Person>
        {
            CreateParent(
                personId: motherId,
                spouseId: fatherId,
                householdId: householdId,
                sex: Sex.Female,
                birthDate: new DateOnly(2020, 5, 1)),
            CreateParent(
                personId: fatherId,
                spouseId: motherId,
                householdId: householdId,
                sex: Sex.Male,
                birthDate: new DateOnly(2018, 5, 1))
        };

        for (int i = 0; i < 10; i++)
        {
            residents.Add(
                CreateChildResident(
                    personId: PersonId.From(CreateGuid(100_000 + i)),
                    householdId: householdId,
                    birthDate: new DateOnly(2038, 5, 1).AddYears(i % 5)));
        }

        IReadOnlyList<CityBirthAutonomyDecision> decisions = policy.Plan(
            residents: residents,
            housingStatuses: new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Housed
            },
            previousDate: new DateOnly(2047, 11, 1),
            currentDate: currentDate);

        Assert.Empty(decisions);
    }

    private static CityBirthAutonomyPolicy CreatePolicy()
    {
        return new CityBirthAutonomyPolicy(
            contentCatalog: new TestPopulationGenerationContentCatalog(
                maleFirstNames: ["Mikhail"],
                femaleFirstNames: ["Anna"]),
            householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
    }

    private static Person CreateParent(
        PersonId personId,
        PersonId spouseId,
        HouseholdId householdId,
        Sex sex,
        DateOnly birthDate,
        int happiness = 90,
        int health = 90,
        int stress = 10,
        int socialNeed = 80,
        int sociability = 90,
        DateOnly? lastChildbirthDate = null)
    {
        return Person.CreatePerson(
            id: personId,
            householdId: householdId,
            name: new PersonName(sex == Sex.Female ? "Anna" : "Mikhail", "Petrov"),
            sex: sex,
            lifeStatus: LifeStatus.Alive,
            maritalStatus: MaritalStatus.Married,
            spouseId: spouseId,
            educationLevel: EducationLevel.Postgraduate,
            educationInstitutionId: null,
            educationInstitutionAnchorId: null,
            employmentStatus: EmploymentStatus.Employed,
            happinessLevel: HappinessLevel.From(happiness),
            energyLevel: EnergyLevel.From(90),
            stressLevel: StressLevel.From(stress),
            socialNeedLevel: SocialNeedLevel.From(socialNeed),
            personality: Personality.Create(
                optimism: 100,
                discipline: 100,
                riskTolerance: 50,
                sociability: sociability),
            birthDate: birthDate,
            healthLevel: HealthLevel.From(health),
            weight: BodyWeight.FromKilograms(70m),
            job: new Job(
                workplaceId: WorkplaceId.From(Guid.Parse("77777777-aaaa-bbbb-cccc-777777777777")),
                title: "Engineer",
                workplaceAnchorId: CityAnchorId.From(Guid.Parse("88888888-aaaa-bbbb-cccc-888888888888"))),
            currentDate: new DateOnly(2048, 5, 1),
            illness: IllnessInfo.Healthy(),
            lastChildbirthDate: lastChildbirthDate);
    }

    private static Person CreateChildResident(
        PersonId personId,
        HouseholdId householdId,
        DateOnly birthDate)
    {
        return Person.CreatePerson(
            id: personId,
            householdId: householdId,
            name: new PersonName("Child", "Petrov"),
            sex: Sex.Male,
            lifeStatus: LifeStatus.Alive,
            maritalStatus: MaritalStatus.Single,
            spouseId: null,
            educationLevel: EducationLevel.None,
            educationInstitutionId: null,
            educationInstitutionAnchorId: null,
            employmentStatus: EmploymentStatus.None,
            happinessLevel: HappinessLevel.From(50),
            energyLevel: EnergyLevel.From(70),
            stressLevel: StressLevel.From(20),
            socialNeedLevel: SocialNeedLevel.From(35),
            personality: Personality.Neutral(),
            birthDate: birthDate,
            healthLevel: HealthLevel.From(90),
            weight: BodyWeight.FromKilograms(35m),
            job: null,
            currentDate: new DateOnly(2048, 5, 1),
            illness: IllnessInfo.Healthy());
    }

    private static Guid CreateGuid(int seed)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
    }

    private sealed class TestPopulationGenerationContentCatalog(
        IReadOnlyList<string> maleFirstNames,
        IReadOnlyList<string> femaleFirstNames) : IPopulationGenerationContentCatalog
    {
        public IReadOnlyList<string> MaleFirstNames => maleFirstNames;
        public IReadOnlyList<string> FemaleFirstNames => femaleFirstNames;
        public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames => [];
        public IReadOnlyList<PopulationProfessionCatalogItem> Professions => [];
    }
}
