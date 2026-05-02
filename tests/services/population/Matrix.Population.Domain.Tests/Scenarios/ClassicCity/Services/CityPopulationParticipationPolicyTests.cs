using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityPopulationParticipationPolicyTests
{
    [Fact]
    public void ResolveEmploymentProfile_WhenResidentIsNotEmployed_ReturnsFullProfile()
    {
        var policy = new CityPopulationParticipationPolicy();
        Matrix.Population.Domain.Entities.Person resident = PopulationTestData.CreateAdultPerson();

        CityPopulationParticipationProfile profile = policy.ResolveEmploymentProfile(
            person: resident,
            currentDate: new DateOnly(2048, 5, 2),
            housingStatus: HousingStatus.Housed,
            livingConditions: CreateLivingConditions(),
            essentials: CreateEssentials());

        Assert.Equal(CityPopulationParticipationProfile.Full, profile);
    }

    [Fact]
    public void ResolveEmploymentProfile_WhenCommuteAndLivingConditionsDegrade_ReducesAllEmploymentIndexes()
    {
        var policy = new CityPopulationParticipationPolicy();
        var currentDate = new DateOnly(2048, 5, 2);

        Matrix.Population.Domain.Entities.Person employedResident = PopulationTestData.CreateAdultPerson(
            currentDate: currentDate);
        employedResident.AssignJob(
            currentDate: currentDate,
            job: PopulationTestData.CreateJob("Engineer"));
        employedResident.ChangeEnergy(-50);
        employedResident.ChangeStress(50);
        employedResident.ChangeHealth(-45, currentDate);
        employedResident.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Moderate,
            currentDate: currentDate);

        CityPopulationParticipationProfile profile = policy.ResolveEmploymentProfile(
            person: employedResident,
            currentDate: currentDate,
            housingStatus: HousingStatus.Homeless,
            livingConditions: new CityPopulationLivingConditionsContext(
                FloodingIndex: 0.8m,
                RoadAccessibilityIndex: 0.4m,
                PowerCoverageIndex: 0.55m,
                UtilityContinuityIndex: 0.5m,
                HeatingCoverageIndex: 0.65m,
                WaterCoverageIndex: 0.6m,
                SanitationCoverageIndex: 0.7m),
            essentials: new CityPopulationEssentialsContext(
                SupplyStressIndex: 1.3m,
                EmergencyRationingEnabled: true,
                FoodStockLevelIndex: 0.7m,
                FoodShortageRiskIndex: 0.8m,
                MedicineStockLevelIndex: 0.7m,
                MedicineShortageRiskIndex: 0.6m,
                EmergencyWaterStockLevelIndex: 0.75m,
                EmergencyWaterShortageRiskIndex: 0.5m),
            commute: CityPopulationCommuteContext.Blocked);

        Assert.Equal(0.20m, profile.AttendanceIndex);
        Assert.Equal(0.25m, profile.ProductivityIndex);
        Assert.Equal(0.25m, profile.PayrollMultiplier);
    }

    [Fact]
    public void ResolveStudentAttendanceIndex_WhenStudentFacesBlockedCommuteAndShortages_ReturnsReducedAttendance()
    {
        var policy = new CityPopulationParticipationPolicy();
        var currentDate = new DateOnly(2048, 5, 2);

        Matrix.Population.Domain.Entities.Person student = PopulationTestData.CreateAdultPerson(
            birthDate: new DateOnly(2038, 5, 1),
            currentDate: currentDate);
        student.StartStudying(
            currentDate: currentDate,
            institutionId: PopulationTestData.CreateEducationInstitutionId(),
            institutionAnchorId: PopulationTestData.CreateCityAnchorId());
        student.ChangeEnergy(-45);
        student.ChangeStress(40);
        student.ChangeHealth(-40, currentDate);

        decimal attendanceIndex = policy.ResolveStudentAttendanceIndex(
            person: student,
            currentDate: currentDate,
            housingStatus: HousingStatus.Homeless,
            livingConditions: new CityPopulationLivingConditionsContext(
                FloodingIndex: 0.7m,
                RoadAccessibilityIndex: 0.5m,
                PowerCoverageIndex: 0.6m,
                UtilityContinuityIndex: 0.7m,
                HeatingCoverageIndex: 0.65m,
                WaterCoverageIndex: 0.7m,
                SanitationCoverageIndex: 0.8m),
            essentials: new CityPopulationEssentialsContext(
                SupplyStressIndex: 1.2m,
                EmergencyRationingEnabled: true,
                FoodStockLevelIndex: 0.8m,
                FoodShortageRiskIndex: 0.7m,
                MedicineStockLevelIndex: 0.8m,
                MedicineShortageRiskIndex: 0.5m,
                EmergencyWaterStockLevelIndex: 0.8m,
                EmergencyWaterShortageRiskIndex: 0.5m),
            commute: CityPopulationCommuteContext.Blocked);

        Assert.Equal(0.18m, attendanceIndex);
    }

    private static CityPopulationLivingConditionsContext CreateLivingConditions()
    {
        return new CityPopulationLivingConditionsContext(
            FloodingIndex: 0m,
            RoadAccessibilityIndex: 1m,
            PowerCoverageIndex: 1m,
            UtilityContinuityIndex: 1m,
            HeatingCoverageIndex: 1m,
            WaterCoverageIndex: 1m,
            SanitationCoverageIndex: 1m);
    }

    private static CityPopulationEssentialsContext CreateEssentials()
    {
        return new CityPopulationEssentialsContext(
            SupplyStressIndex: 1m,
            EmergencyRationingEnabled: false,
            FoodStockLevelIndex: 1m,
            FoodShortageRiskIndex: 0m,
            MedicineStockLevelIndex: 1m,
            MedicineShortageRiskIndex: 0m,
            EmergencyWaterStockLevelIndex: 1m,
            EmergencyWaterShortageRiskIndex: 0m);
    }
}
