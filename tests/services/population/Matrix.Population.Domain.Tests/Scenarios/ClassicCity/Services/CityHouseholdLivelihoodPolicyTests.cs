using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdLivelihoodPolicyTests
    {
        [Fact]
        public void Build_WhenNoAliveResidents_ReturnsZeroProfileWithProvidedHousingStatus()
        {
            var policy = new CityHouseholdLivelihoodPolicy();
            Person deceasedResident = PopulationTestData.CreateAdultPerson();
            deceasedResident.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            CityHouseholdLivelihoodProfile profile = policy.Build(
                householdResidents: [deceasedResident],
                housingStatus: HousingStatus.Housed,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 2));

            Assert.Equal(
                expected: HousingStatus.Housed,
                actual: profile.HousingStatus);
            Assert.Equal(
                expected: 0,
                actual: profile.ResidentCount);
            Assert.Equal(
                expected: 0,
                actual: profile.AdultProviderCount);
            Assert.Equal(
                expected: 0,
                actual: profile.AdultStudentCount);
            Assert.Equal(
                expected: 0,
                actual: profile.DependentCount);
            Assert.Equal(
                expected: 0,
                actual: profile.InfantCount);
            Assert.Equal(
                expected: 0,
                actual: profile.ActiveIllnessCount);
            Assert.Equal(
                expected: 0d,
                actual: profile.StabilityScore);
            Assert.True(profile.IsHoused);
            Assert.False(profile.HasStructuredSupport);
        }

        [Fact]
        public void Build_WhenHouseholdHasMixedResidents_BuildsCountsAndClampedStability()
        {
            var policy = new CityHouseholdLivelihoodPolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);

            Person employedAdult = PopulationTestData.CreateAdultPerson(
                householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"));
            employedAdult.AssignJob(
                currentDate: currentDate,
                job: PopulationTestData.CreateJob("Architect"));
            employedAdult.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Mild,
                currentDate: currentDate);

            Person adultStudent = PopulationTestData.CreateAdultPerson(
                firstName: "Olga",
                lastName: "Ivanova",
                sex: Sex.Female,
                personId: Guid.Parse("88888888-1111-1111-1111-111111111111"),
                householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"));
            adultStudent.StartStudying(
                currentDate: currentDate,
                institutionId: PopulationTestData.CreateEducationInstitutionId(),
                institutionAnchorId: PopulationTestData.CreateCityAnchorId());

            Person child = PopulationTestData.CreateAdultPerson(
                firstName: "Petr",
                lastName: "Ivanov",
                personId: Guid.Parse("99999999-1111-1111-1111-111111111111"),
                householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                birthDate: new DateOnly(
                    year: 2040,
                    month: 1,
                    day: 1));

            Person infant = PopulationTestData.CreateAdultPerson(
                firstName: "Mila",
                lastName: "Ivanova",
                sex: Sex.Female,
                personId: Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
                householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                birthDate: currentDate,
                currentDate: currentDate);

            Person deceasedResident = PopulationTestData.CreateAdultPerson(
                firstName: "Stepan",
                lastName: "Ivanov",
                personId: Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"),
                householdId: Guid.Parse("33333333-3333-3333-3333-333333333333"));
            deceasedResident.Die(currentDate);

            CityHouseholdLivelihoodProfile profile = policy.Build(
                householdResidents:
                [
                    employedAdult,
                    adultStudent,
                    child,
                    infant,
                    deceasedResident
                ],
                housingStatus: HousingStatus.Housed,
                currentDate: currentDate);

            Assert.Equal(
                expected: 4,
                actual: profile.ResidentCount);
            Assert.Equal(
                expected: 1,
                actual: profile.AdultProviderCount);
            Assert.Equal(
                expected: 1,
                actual: profile.AdultStudentCount);
            Assert.Equal(
                expected: 2,
                actual: profile.DependentCount);
            Assert.Equal(
                expected: 1,
                actual: profile.InfantCount);
            Assert.Equal(
                expected: 1,
                actual: profile.ActiveIllnessCount);
            Assert.InRange(
                actual: profile.AverageHealth,
                low: 70d,
                high: 90d);
            Assert.InRange(
                actual: profile.AverageEnergy,
                low: 60d,
                high: 80d);
            Assert.InRange(
                actual: profile.AverageStress,
                low: 20d,
                high: 30d);
            Assert.InRange(
                actual: profile.StabilityScore,
                low: 0d,
                high: 1d);
            Assert.True(profile.HasStructuredSupport);
        }

        [Fact]
        public void ResolveResidentSelfReliance_WhenResidentHasEmploymentAndBetterCondition_IsHigher()
        {
            var policy = new CityHouseholdLivelihoodPolicy();
            var currentDate = new DateOnly(
                year: 2048,
                month: 5,
                day: 2);

            Person employedResident = PopulationTestData.CreateAdultPerson();
            employedResident.AssignJob(
                currentDate: currentDate,
                job: PopulationTestData.CreateJob());

            Person unemployedResident = PopulationTestData.CreateAdultPerson(
                firstName: "Sergey",
                lastName: "Petrov",
                personId: Guid.Parse("cccccccc-1111-1111-1111-111111111111"));
            unemployedResident.TryApplyHealthcareOutcome(
                sourceRevision: 0,
                healthScore: 40,
                illness: IllnessInfo.Healthy(),
                happinessDelta: 0,
                energyDelta: 0,
                stressDelta: 0,
                currentDate: currentDate);
            unemployedResident.ChangeEnergy(-45);
            unemployedResident.ChangeStress(55);

            double employedSelfReliance = policy.ResolveResidentSelfReliance(employedResident);
            double unemployedSelfReliance = policy.ResolveResidentSelfReliance(unemployedResident);

            Assert.InRange(
                actual: employedSelfReliance,
                low: 0d,
                high: 1d);
            Assert.InRange(
                actual: unemployedSelfReliance,
                low: 0d,
                high: 1d);
            Assert.True(employedSelfReliance > unemployedSelfReliance);
        }
    }
}
