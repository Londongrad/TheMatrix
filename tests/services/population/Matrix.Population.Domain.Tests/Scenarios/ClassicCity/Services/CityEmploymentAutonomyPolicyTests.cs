using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityEmploymentAutonomyPolicyTests
    {
        [Fact]
        public void Constructor_WhenProfessionCatalogIsEmpty_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new CityEmploymentAutonomyPolicy(
                    contentCatalog: new TestPopulationGenerationContentCatalog(professions: []),
                    householdEconomyPolicy: CreateHouseholdEconomyPolicy(),
                    anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy()));
        }

        [Fact]
        public void Apply_WhenResidentIsNotAdult_ReturnsFalse()
        {
            CityEmploymentAutonomyPolicy policy = CreatePolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);
            Person youth = CreatePerson(
                personId: Guid.Parse("10101010-aaaa-bbbb-cccc-111111111111"),
                birthDate: new DateOnly(
                    year: 2034,
                    month: 5,
                    day: 1),
                currentDate: currentDate,
                educationLevel: EducationLevel.UpperSecondary,
                employmentStatus: EmploymentStatus.Unemployed);
            Household household = PopulationTestData.CreateHousehold(cashReserve: -200m);

            bool changed = policy.Apply(
                person: youth,
                household: household,
                householdResidents: [youth],
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                previousDate: new DateOnly(
                    year: 2048,
                    month: 4,
                    day: 1),
                currentDate: currentDate,
                housingStatus: HousingStatus.Housed,
                preferredDistrictId: null,
                workplaceAnchors: [CreateWorkplaceAnchor()],
                workplacePools: new Dictionary<string, List<Job>>(),
                employerStressByWorkplaceId: new Dictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>());

            Assert.False(changed);
            Assert.Equal(
                expected: EmploymentStatus.Unemployed,
                actual: youth.Employment.Status);
        }

        [Fact]
        public void Apply_WhenAdultHasStrongNeedForWork_AssignsJobAndCreatesPoolEntry()
        {
            CityEmploymentAutonomyPolicy policy = CreatePolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);
            Household household = PopulationTestData.CreateHousehold(cashReserve: -500m);
            Person adult = CreatePerson(
                personId: Guid.Parse("9d4d5f12-7f2f-4c1e-88d8-a9f3df448c0f"),
                birthDate: new DateOnly(
                    year: 2020,
                    month: 5,
                    day: 1),
                currentDate: currentDate,
                educationLevel: EducationLevel.Postgraduate,
                employmentStatus: EmploymentStatus.Unemployed,
                personality: Personality.Create(
                    optimism: 100,
                    discipline: 100,
                    riskTolerance: 50,
                    sociability: 50),
                health: 100,
                energy: 100,
                stress: 0);
            Dictionary<string, List<Job>> workplacePools = [];
            CityPopulationAnchorCatalogItem workplaceAnchor = CreateWorkplaceAnchor();

            bool changed = policy.Apply(
                person: adult,
                household: household,
                householdResidents: [adult],
                routineProfilesByResidentId: new Dictionary<PersonId, PersonRoutineProfile>(),
                previousDate: new DateOnly(
                    year: 2047,
                    month: 1,
                    day: 1),
                currentDate: currentDate,
                housingStatus: HousingStatus.Housed,
                preferredDistrictId: workplaceAnchor.DistrictId,
                workplaceAnchors: [workplaceAnchor],
                workplacePools: workplacePools,
                employerStressByWorkplaceId: new Dictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>(),
                preferredWorkplaceAnchorIds: [workplaceAnchor.CityAnchorId],
                costOfLivingState: PopulationTestData.CreateCostOfLivingState());

            Assert.True(changed);
            Assert.Equal(
                expected: EmploymentStatus.Employed,
                actual: adult.Employment.Status);
            Assert.True(workplacePools.ContainsKey("Engineer"));
            Job createdJob = Assert.Single(workplacePools["Engineer"]);
            Assert.Equal(
                expected: "Engineer",
                actual: createdJob.Title);
            Assert.Equal(
                expected: workplaceAnchor.CityAnchorId,
                actual: createdJob.WorkplaceAnchorId);
        }

        private static CityEmploymentAutonomyPolicy CreatePolicy()
        {
            return new CityEmploymentAutonomyPolicy(
                contentCatalog: new TestPopulationGenerationContentCatalog(
                    professions:
                    [
                        new PopulationProfessionCatalogItem(
                            Title: "Engineer",
                            Weight: 1)
                    ]),
                householdEconomyPolicy: CreateHouseholdEconomyPolicy(),
                anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy());
        }

        private static CityHouseholdEconomyPolicy CreateHouseholdEconomyPolicy()
        {
            return new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy(),
                householdCashflowPolicy: new CityHouseholdCashflowPolicy());
        }

        private static Person CreatePerson(
            Guid personId,
            DateOnly birthDate,
            DateOnly currentDate,
            EducationLevel educationLevel,
            EmploymentStatus employmentStatus,
            Personality? personality = null,
            int health = 80,
            int energy = 70,
            int stress = 25)
        {
            return Person.CreatePerson(
                id: PersonId.From(personId),
                householdId: HouseholdId.From(Guid.Parse("21212121-3434-5656-7878-909090909090")),
                name: new PersonName(
                    firstName: "Ivan",
                    lastName: "Ivanov"),
                sex: Sex.Male,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                spouseId: null,
                educationLevel: educationLevel,
                educationInstitutionId: null,
                educationInstitutionAnchorId: null,
                employmentStatus: employmentStatus,
                happinessLevel: HappinessLevel.From(50),
                energyLevel: EnergyLevel.From(energy),
                stressLevel: StressLevel.From(stress),
                socialNeedLevel: SocialNeedLevel.From(35),
                personality: personality ?? Personality.Neutral(),
                birthDate: birthDate,
                healthLevel: HealthLevel.From(health),
                weight: BodyWeight.FromKilograms(70m),
                job: null,
                currentDate: currentDate);
        }

        private static CityPopulationAnchorCatalogItem CreateWorkplaceAnchor()
        {
            return CityPopulationAnchorCatalogItem.Create(
                cityId: CityId.From(Guid.Parse("45454545-6767-8989-1010-121212121212")),
                cityAnchorId: CityAnchorId.From(Guid.Parse("56565656-7878-9090-1111-131313131313")),
                districtId: DistrictId.From(Guid.Parse("67676767-8989-1010-1212-141414141414")),
                accessRoadNodeId: RoadNodeId.From(Guid.Parse("78787878-9090-1111-1313-151515151515")),
                name: "Workplace",
                type: CityAnchorType.Workplace,
                capacity: 100,
                positionX: 10m,
                positionY: 20m,
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 2,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        private sealed class TestPopulationGenerationContentCatalog(
            IReadOnlyList<PopulationProfessionCatalogItem> professions) : IPopulationGenerationContentCatalog
        {
            public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
            public IReadOnlyList<string> FemaleFirstNames => ["Anna"];
            public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames => [];
            public IReadOnlyList<PopulationProfessionCatalogItem> Professions => professions;
        }
    }
}
