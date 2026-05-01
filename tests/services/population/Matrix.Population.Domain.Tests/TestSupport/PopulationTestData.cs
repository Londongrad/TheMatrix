using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Tests.TestSupport;

internal static class PopulationTestData
{
    internal static Person CreateAdultPerson(
        DateOnly? currentDate = null,
        HappinessLevel? happiness = null)
    {
        DateOnly resolvedCurrentDate = currentDate ?? new DateOnly(2048, 5, 1);

        return Person.CreatePerson(
            id: PersonId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            householdId: HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            name: new PersonName("Ivan", "Ivanov"),
            sex: Sex.Male,
            lifeStatus: LifeStatus.Alive,
            maritalStatus: MaritalStatus.Single,
            spouseId: null,
            educationLevel: EducationLevel.UpperSecondary,
            educationInstitutionId: null,
            educationInstitutionAnchorId: null,
            employmentStatus: EmploymentStatus.Unemployed,
            happinessLevel: happiness ?? HappinessLevel.From(50),
            energyLevel: EnergyLevel.From(70),
            stressLevel: StressLevel.From(25),
            socialNeedLevel: SocialNeedLevel.From(35),
            personality: Personality.Neutral(),
            birthDate: new DateOnly(2030, 4, 2),
            healthLevel: HealthLevel.From(80),
            weight: BodyWeight.FromKilograms(72m),
            job: null,
            currentDate: resolvedCurrentDate,
            illness: IllnessInfo.Healthy());
    }

    internal static Household CreateHousehold(
        decimal cashReserve = 100m,
        DateTimeOffset? createdAtUtc = null)
    {
        return Household.Create(
            id: HouseholdId.From(Guid.Parse("33333333-3333-3333-3333-333333333333")),
            size: HouseholdSize.From(3),
            createdAtUtc: createdAtUtc ?? new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero),
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
}
