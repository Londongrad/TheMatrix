using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityEducationAutonomyPolicyTests
{
    [Fact]
    public void Apply_WhenResidentIsDead_ReturnsFalse()
    {
        var policy = new CityEducationAutonomyPolicy(
            anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy());
        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson();
        deceasedResident.Die(new DateOnly(2048, 5, 2));

        bool changed = policy.Apply(
            person: deceasedResident,
            previousDate: new DateOnly(2048, 5, 1),
            currentDate: new DateOnly(2048, 5, 2),
            institutionPools: new Dictionary<EducationLevel, List<CityEducationInstitutionBinding>>(),
            preferredDistrictId: null,
            schoolAnchors: [CreateSchoolAnchor()]);

        Assert.False(changed);
    }

    [Fact]
    public void Apply_WhenChildHasNoEducation_GraduatesToPreschoolAndStartsStudying()
    {
        var policy = new CityEducationAutonomyPolicy(
            anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy());
        var currentDate = new DateOnly(2048, 5, 2);
        Matrix.Population.Domain.Entities.Person child = CreatePerson(
            personId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            birthDate: new DateOnly(2043, 5, 1),
            currentDate: currentDate,
            educationLevel: EducationLevel.None,
            employmentStatus: EmploymentStatus.None);
        Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> pools = [];

        bool changed = policy.Apply(
            person: child,
            previousDate: new DateOnly(2048, 5, 1),
            currentDate: currentDate,
            institutionPools: pools,
            preferredDistrictId: null,
            schoolAnchors: [CreateSchoolAnchor()],
            preferredInstitutionAnchorIds: [PopulationTestData.CreateCityAnchorId()]);

        Assert.True(changed);
        Assert.Equal(EducationLevel.Preschool, child.EducationLevel);
        Assert.Equal(EmploymentStatus.Student, child.Employment.Status);
        Assert.NotNull(child.Education.CurrentInstitutionId);
        Assert.True(pools.ContainsKey(EducationLevel.Preschool));
        Assert.Single(pools[EducationLevel.Preschool]);
    }

    [Fact]
    public void Apply_WhenPostgraduateStudentIsOldEnough_StopsStudying()
    {
        var policy = new CityEducationAutonomyPolicy(
            anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy());
        var currentDate = new DateOnly(2048, 5, 2);
        Matrix.Population.Domain.Entities.Person postgraduateStudent = CreatePerson(
            personId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
            birthDate: new DateOnly(2016, 5, 1),
            currentDate: currentDate,
            educationLevel: EducationLevel.Postgraduate,
            employmentStatus: EmploymentStatus.Student,
            institutionId: PopulationTestData.CreateEducationInstitutionId(),
            institutionAnchorId: PopulationTestData.CreateCityAnchorId());

        bool changed = policy.Apply(
            person: postgraduateStudent,
            previousDate: new DateOnly(2048, 4, 1),
            currentDate: currentDate,
            institutionPools: new Dictionary<EducationLevel, List<CityEducationInstitutionBinding>>(),
            preferredDistrictId: null,
            schoolAnchors: [CreateSchoolAnchor()]);

        Assert.True(changed);
        Assert.Equal(EmploymentStatus.Unemployed, postgraduateStudent.Employment.Status);
    }

    private static Person CreatePerson(
        Guid personId,
        DateOnly birthDate,
        DateOnly currentDate,
        EducationLevel educationLevel,
        EmploymentStatus employmentStatus,
        EducationInstitutionId? institutionId = null,
        CityAnchorId? institutionAnchorId = null)
    {
        return Person.CreatePerson(
            id: PersonId.From(personId),
            householdId: HouseholdId.From(Guid.Parse("12121212-3434-5656-7878-909090909090")),
            name: new PersonName("Ivan", "Ivanov"),
            sex: Sex.Male,
            lifeStatus: LifeStatus.Alive,
            maritalStatus: MaritalStatus.Single,
            spouseId: null,
            educationLevel: educationLevel,
            educationInstitutionId: institutionId,
            educationInstitutionAnchorId: institutionAnchorId,
            employmentStatus: employmentStatus,
            happinessLevel: HappinessLevel.From(50),
            energyLevel: EnergyLevel.From(70),
            stressLevel: StressLevel.From(25),
            socialNeedLevel: SocialNeedLevel.From(35),
            personality: Personality.Neutral(),
            birthDate: birthDate,
            healthLevel: HealthLevel.From(80),
            weight: BodyWeight.FromKilograms(70m),
            job: null,
            currentDate: currentDate,
            illness: IllnessInfo.Healthy());
    }

    private static CityPopulationAnchorCatalogItem CreateSchoolAnchor()
    {
        return CityPopulationAnchorCatalogItem.Create(
            cityId: CityId.From(Guid.Parse("abababab-abab-abab-abab-abababababab")),
            cityAnchorId: PopulationTestData.CreateCityAnchorId(),
            districtId: DistrictId.From(Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd")),
            accessRoadNodeId: RoadNodeId.From(Guid.Parse("efefefef-efef-efef-efef-efefefefefef")),
            name: "School",
            type: CityAnchorType.School,
            capacity: 100,
            positionX: 10m,
            positionY: 20m,
            createdAtUtc: new DateTimeOffset(2048, 5, 2, 0, 0, 0, TimeSpan.Zero));
    }
}
