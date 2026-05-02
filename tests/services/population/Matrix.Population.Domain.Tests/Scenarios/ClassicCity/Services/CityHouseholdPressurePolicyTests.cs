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

public sealed class CityHouseholdPressurePolicyTests
{
    [Fact]
    public void Apply_WhenResidentIsDeadOrIntervalDoesNotAdvance_ReturnsFalseWithoutMutation()
    {
        var policy = new CityHouseholdPressurePolicy();
        Matrix.Population.Domain.Entities.Person deceasedResident = PopulationTestData.CreateAdultPerson();
        deceasedResident.Die(new DateOnly(2048, 5, 2));

        bool deceasedResult = policy.Apply(
            resident: deceasedResident,
            householdResidents: [deceasedResident],
            housingStatus: HousingStatus.Housed,
            financialStressState: null,
            commutePressureProfile: null,
            previousDate: new DateOnly(2048, 5, 1),
            currentDate: new DateOnly(2048, 5, 2));

        Assert.False(deceasedResult);

        Matrix.Population.Domain.Entities.Person aliveResident = PopulationTestData.CreateAdultPerson();
        int previousHappiness = aliveResident.Happiness.Value;
        int previousEnergy = aliveResident.Energy.Value;
        int previousStress = aliveResident.Stress.Value;
        int previousSocialNeed = aliveResident.SocialNeed.Value;

        bool nonAdvancingResult = policy.Apply(
            resident: aliveResident,
            householdResidents: [aliveResident],
            housingStatus: HousingStatus.Housed,
            financialStressState: null,
            commutePressureProfile: null,
            previousDate: new DateOnly(2048, 5, 2),
            currentDate: new DateOnly(2048, 5, 2));

        Assert.False(nonAdvancingResult);
        Assert.Equal(previousHappiness, aliveResident.Happiness.Value);
        Assert.Equal(previousEnergy, aliveResident.Energy.Value);
        Assert.Equal(previousStress, aliveResident.Stress.Value);
        Assert.Equal(previousSocialNeed, aliveResident.SocialNeed.Value);
    }

    [Fact]
    public void Apply_WhenResidentHasCommuteAndRecentFinancialStress_AppliesExpectedPressure()
    {
        var policy = new CityHouseholdPressurePolicy();
        var currentDate = new DateOnly(2048, 5, 3);

        Matrix.Population.Domain.Entities.Person resident = PopulationTestData.CreateAdultPerson(
            currentDate: currentDate);
        resident.AssignJob(
            currentDate: currentDate,
            job: PopulationTestData.CreateJob("Architect"));
        resident.DiagnoseIllness(
            kind: IllnessKind.Infection,
            severity: IllnessSeverity.Mild,
            currentDate: currentDate);

        bool changed = policy.Apply(
            resident: resident,
            householdResidents: [resident],
            housingStatus: HousingStatus.Homeless,
            financialStressState: CreateFinancialStressState(
                householdId: resident.HouseholdId,
                lastEvaluatedAtUtc: new DateTimeOffset(2048, 5, 2, 0, 0, 0, TimeSpan.Zero)),
            commutePressureProfile: new CityHouseholdCommutePressureProfile(
                RoutedResidentCount: 1,
                BlockedRouteCount: 1,
                AccessibilityDeficitIndex: 0.6m,
                TravelFatigueIndex: 0.8m),
            previousDate: new DateOnly(2048, 5, 2),
            currentDate: currentDate);

        Assert.True(changed);
        Assert.Equal(46, resident.Happiness.Value);
        Assert.Equal(63, resident.Energy.Value);
        Assert.Equal(45, resident.Stress.Value);
        Assert.Equal(37, resident.SocialNeed.Value);
    }

    [Fact]
    public void Apply_WhenFinancialStressIsStale_IgnoresStressSnapshot()
    {
        var policy = new CityHouseholdPressurePolicy();
        var currentDate = new DateOnly(2048, 5, 3);

        Matrix.Population.Domain.Entities.Person resident = PopulationTestData.CreateAdultPerson(
            currentDate: currentDate);

        bool changed = policy.Apply(
            resident: resident,
            householdResidents: [resident],
            housingStatus: HousingStatus.Housed,
            financialStressState: CreateFinancialStressState(
                householdId: resident.HouseholdId,
                lastEvaluatedAtUtc: new DateTimeOffset(2048, 2, 1, 0, 0, 0, TimeSpan.Zero)),
            commutePressureProfile: null,
            previousDate: new DateOnly(2048, 5, 2),
            currentDate: currentDate);

        Assert.True(changed);
        Assert.Equal(49, resident.Happiness.Value);
        Assert.Equal(70, resident.Energy.Value);
        Assert.Equal(25, resident.Stress.Value);
        Assert.Equal(37, resident.SocialNeed.Value);
    }

    private static CityPopulationHouseholdFinancialStressState CreateFinancialStressState(
        HouseholdId householdId,
        DateTimeOffset lastEvaluatedAtUtc)
    {
        return CityPopulationHouseholdFinancialStressState.Create(
            cityId: CityId.From(Guid.Parse("45454545-4545-4545-4545-454545454545")),
            householdId: householdId,
            overdueObligationCount: 3,
            overdueRentCount: 1,
            overdueUtilityCount: 1,
            arrearsObligationCount: 1,
            serviceCutoffCount: 1,
            evictionNoticeCount: 1,
            evictionEligibleCount: 1,
            oldestOverdueAgeDays: 60,
            totalOverdueAmount: 250m,
            distressScore: 0.65m,
            lastEvaluatedAtUtc: lastEvaluatedAtUtc,
            updatedAtUtc: lastEvaluatedAtUtc);
    }
}
