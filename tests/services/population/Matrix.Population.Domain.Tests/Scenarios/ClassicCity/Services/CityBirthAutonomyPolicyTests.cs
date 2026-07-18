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

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
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
            CityBirthAutonomyPolicy policy = CreatePolicy();
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 1);
            var motherId = PersonId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"));
            var fatherId = PersonId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));
            var householdId = HouseholdId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-333333333333"));
            Person mother = CreateParent(
                personId: motherId,
                spouseId: fatherId,
                householdId: householdId,
                sex: Sex.Female,
                birthDate: new DateOnly(
                    year: 2020,
                    month: 5,
                    day: 1),
                lastChildbirthDate: new DateOnly(
                    year: 2047,
                    month: 9,
                    day: 15));
            Person father = CreateParent(
                personId: fatherId,
                spouseId: motherId,
                householdId: householdId,
                sex: Sex.Male,
                birthDate: new DateOnly(
                    year: 2018,
                    month: 5,
                    day: 1));

            IReadOnlyList<CityBirthAutonomyDecision> decisions = policy.Plan(
                residents:
                [
                    mother,
                    father
                ],
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Housed
                },
                previousDate: new DateOnly(
                    year: 2047,
                    month: 11,
                    day: 1),
                currentDate: currentDate);

            Assert.Empty(decisions);
        }

        [Fact]
        public void Plan_WhenStableMarriedCoupleRollBirth_ReturnsDecisionWithNewbornProfile()
        {
            CityBirthAutonomyPolicy policy = CreatePolicy();
            DateOnly previousDate = new(
                year: 2047,
                month: 11,
                day: 1);
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 1);

            for (int seed = 1; seed <= 2_000; seed++)
            {
                var motherId = PersonId.From(CreateGuid(70_000 + seed));
                var fatherId = PersonId.From(CreateGuid(80_000 + seed));
                var householdId = HouseholdId.From(CreateGuid(90_000 + seed));
                Person mother = CreateParent(
                    personId: motherId,
                    spouseId: fatherId,
                    householdId: householdId,
                    sex: Sex.Female,
                    birthDate: new DateOnly(
                        year: 2020,
                        month: 5,
                        day: 1),
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
                    birthDate: new DateOnly(
                        year: 2018,
                        month: 5,
                        day: 1),
                    happiness: 100,
                    health: 100,
                    stress: 0,
                    socialNeed: 100,
                    sociability: 100);
                IReadOnlyList<CityBirthAutonomyDecision> decisions = policy.Plan(
                    residents:
                    [
                        mother,
                        father
                    ],
                    routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                    housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                    {
                        [householdId] = HousingStatus.Housed
                    },
                    previousDate: previousDate,
                    currentDate: currentDate);

                if (decisions.Count == 1)
                {
                    CityBirthAutonomyDecision decision = decisions[0];
                    Assert.Equal(
                        expected: mother.Id,
                        actual: decision.MotherId);
                    Assert.Equal(
                        expected: father.Id,
                        actual: decision.FatherId);
                    Assert.NotEqual(
                        expected: Guid.Empty,
                        actual: decision.Newborn.PersonId.Value);
                    Assert.Equal(
                        expected: "Petrov",
                        actual: decision.Newborn.Name.LastName);
                    Assert.Contains(
                        expected: decision.Newborn.Name.FirstName,
                        collection: new[]
                        {
                            "Anna",
                            "Mikhail"
                        });
                    Assert.InRange(
                        actual: decision.Newborn.Health.Value,
                        low: 75,
                        high: 100);
                    Assert.InRange(
                        actual: decision.Newborn.Weight.Kilograms,
                        low: 3.0m,
                        high: 4.5m);
                    return;
                }
            }

            throw new XunitException("Expected at least one deterministic stable couple to schedule childbirth.");
        }

        [Fact]
        public void Plan_WhenHouseholdIsAlreadyAtMaximumSize_ReturnsEmpty()
        {
            CityBirthAutonomyPolicy policy = CreatePolicy();
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 1);
            var motherId = PersonId.From(Guid.Parse("44444444-aaaa-bbbb-cccc-444444444444"));
            var fatherId = PersonId.From(Guid.Parse("55555555-aaaa-bbbb-cccc-555555555555"));
            var householdId = HouseholdId.From(Guid.Parse("66666666-aaaa-bbbb-cccc-666666666666"));
            var residents = new List<Person>
            {
                CreateParent(
                    personId: motherId,
                    spouseId: fatherId,
                    householdId: householdId,
                    sex: Sex.Female,
                    birthDate: new DateOnly(
                        year: 2020,
                        month: 5,
                        day: 1)),
                CreateParent(
                    personId: fatherId,
                    spouseId: motherId,
                    householdId: householdId,
                    sex: Sex.Male,
                    birthDate: new DateOnly(
                        year: 2018,
                        month: 5,
                        day: 1))
            };

            for (int i = 0; i < 10; i++)
                residents.Add(
                    CreateChildResident(
                        personId: PersonId.From(CreateGuid(100_000 + i)),
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2038,
                            month: 5,
                            day: 1).AddYears(i % 5)));

            IReadOnlyList<CityBirthAutonomyDecision> decisions = policy.Plan(
                residents: residents,
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Housed
                },
                previousDate: new DateOnly(
                    year: 2047,
                    month: 11,
                    day: 1),
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
                name: new PersonName(
                    firstName: sex == Sex.Female
                        ? "Anna"
                        : "Mikhail",
                    lastName: "Petrov"),
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Married,
                spouseId: spouseId,
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
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
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
                name: new PersonName(
                    firstName: "Child",
                    lastName: "Petrov"),
                sex: Sex.Male,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                spouseId: null,
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
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1));
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
}
