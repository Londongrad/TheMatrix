using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Tests.TestSupport
{
    internal static class PopulationTestData
    {
        internal static void ApplyFunctionalCapacityProjection(
            Person person,
            DateOnly currentDate,
            int functionalCapacityScore,
            int? healthScore = null,
            int happinessDelta = 0,
            int energyDelta = 0,
            int stressDelta = 0)
        {
            bool applied = person.TryApplyVitalStateProjection(
                sourceRevision: person.LastVitalStateRevision + 1,
                healthScore: healthScore ?? person.Health.Value,
                happinessDelta: happinessDelta,
                energyDelta: energyDelta,
                stressDelta: stressDelta,
                currentDate: currentDate,
                functionalCapacityScore: functionalCapacityScore);

            if (!applied)
                throw new InvalidOperationException("The functional capacity test projection was not accepted.");
        }

        internal static Person CreateAdultPerson(
            string firstName = "Ivan",
            string lastName = "Ivanov",
            Sex sex = Sex.Male,
            Guid? personId = null,
            Guid? householdId = null,
            DateOnly? birthDate = null,
            DateOnly? currentDate = null,
            HappinessLevel? happiness = null,
            MaritalStatus maritalStatus = MaritalStatus.Single,
            PersonId? spouseId = null)
        {
            DateOnly resolvedCurrentDate = currentDate ??
            new DateOnly(
                year: 2048,
                month: 5,
                day: 1);

            return Person.CreatePerson(
                id: PersonId.From(personId ?? Guid.Parse("11111111-1111-1111-1111-111111111111")),
                householdId: HouseholdId.From(householdId ?? Guid.Parse("22222222-2222-2222-2222-222222222222")),
                name: new PersonName(
                    firstName: firstName,
                    lastName: lastName),
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                spouseId: spouseId,
                educationLevel: EducationLevel.UpperSecondary,
                educationInstitutionId: null,
                educationInstitutionAnchorId: null,
                employmentStatus: EmploymentStatus.Unemployed,
                happinessLevel: happiness ?? HappinessLevel.From(50),
                energyLevel: EnergyLevel.From(70),
                stressLevel: StressLevel.From(25),
                socialNeedLevel: SocialNeedLevel.From(35),
                personality: Personality.Neutral(),
                birthDate: birthDate ??
                new DateOnly(
                    year: 2030,
                    month: 4,
                    day: 2),
                healthLevel: HealthLevel.From(80),
                weight: BodyWeight.FromKilograms(72m),
                job: null,
                currentDate: resolvedCurrentDate);
        }

        internal static Household CreateHousehold(
            decimal cashReserve = 100m,
            DateTimeOffset? createdAtUtc = null)
        {
            return Household.Create(
                id: HouseholdId.From(Guid.Parse("33333333-3333-3333-3333-333333333333")),
                size: HouseholdSize.From(3),
                createdAtUtc: createdAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                cashReserve: Money.FromDecimal(cashReserve));
        }

        internal static EducationInstitutionId CreateEducationInstitutionId()
        {
            return EducationInstitutionId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        }

        internal static CityAnchorId CreateCityAnchorId()
        {
            return CityAnchorId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        }

        internal static Job CreateJob(string title = "Engineer")
        {
            return new Job(
                workplaceId: WorkplaceId.From(Guid.Parse("66666666-6666-6666-6666-666666666666")),
                title: title,
                workplaceAnchorId: CreateCityAnchorId());
        }

        internal static CityPopulationCostOfLivingState CreateCostOfLivingState(
            decimal wageMultiplier = 1m,
            decimal retailPriceMultiplier = 1m,
            decimal housingCostMultiplier = 1m,
            decimal utilityCostMultiplier = 1m,
            decimal costOfLivingIndex = 1m,
            decimal affordabilityIndex = 1m)
        {
            return CityPopulationCostOfLivingState.Create(
                cityId: CityId.From(Guid.Parse("77777777-7777-7777-7777-777777777777")),
                wageMultiplier: wageMultiplier,
                retailPriceMultiplier: retailPriceMultiplier,
                housingCostMultiplier: housingCostMultiplier,
                utilityCostMultiplier: utilityCostMultiplier,
                costOfLivingIndex: costOfLivingIndex,
                affordabilityIndex: affordabilityIndex,
                lastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
