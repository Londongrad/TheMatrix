using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityEconomyServiceQualityPolicyTests
{
    [Fact]
    public void Evaluate_WhenFundingAndBusinessSupportAreStrong_OutperformsStressedScenario()
    {
        Guid cityId = Guid.NewGuid();
        var policy = new CityEconomyServiceQualityPolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyServiceQualitySnapshot strongSnapshot = policy.Evaluate(
            budget: EconomyTestData.CreateBudget(cityId, balance: 40_000m),
            allocations:
            [
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Healthcare, targetAmount: 1_200m, totalSpent: 250m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Education, targetAmount: 1_100m, totalSpent: 220m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Housing, targetAmount: 1_300m, totalSpent: 260m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Operations, targetAmount: 1_000m, totalSpent: 250m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.General, targetAmount: 1_000m, totalSpent: 240m)
            ],
            businesses: CreateHealthyBusinesses(cityId),
            asOfUtc: new DateTimeOffset(2048, 5, 3, 0, 0, 0, TimeSpan.Zero));
        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyServiceQualitySnapshot stressedSnapshot = policy.Evaluate(
            budget: EconomyTestData.CreateBudget(cityId, balance: -90_000m),
            allocations:
            [
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Healthcare, targetAmount: 1_200m, totalSpent: 1_600m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Education, targetAmount: 1_100m, totalSpent: 1_500m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Housing, targetAmount: 1_300m, totalSpent: 1_700m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.Operations, targetAmount: 1_000m, totalSpent: 1_450m),
                EconomyTestData.CreateAllocation(cityId, CityBudgetCategory.General, targetAmount: 1_000m, totalSpent: 1_400m)
            ],
            businesses: CreateStressedBusinesses(cityId),
            asOfUtc: new DateTimeOffset(2048, 5, 3, 0, 0, 0, TimeSpan.Zero));

        Assert.True(strongSnapshot.HealthcareQualityIndex > stressedSnapshot.HealthcareQualityIndex);
        Assert.True(strongSnapshot.EducationQualityIndex > stressedSnapshot.EducationQualityIndex);
        Assert.True(strongSnapshot.HousingSupportIndex > stressedSnapshot.HousingSupportIndex);
    }

    [Fact]
    public void Evaluate_WhenOptionalInputsAreMissing_UsesFallbackValuesAndPreservesTimestamp()
    {
        DateTimeOffset asOfUtc = new(2048, 5, 4, 0, 0, 0, TimeSpan.Zero);
        var policy = new CityEconomyServiceQualityPolicy();

        Matrix.Economy.Domain.Scenarios.ClassicCity.Models.CityEconomyServiceQualitySnapshot snapshot = policy.Evaluate(
            budget: null,
            allocations: [],
            businesses: [],
            asOfUtc: asOfUtc);

        Assert.InRange(snapshot.HealthcareQualityIndex, 0.45m, 1.55m);
        Assert.InRange(snapshot.EducationQualityIndex, 0.45m, 1.55m);
        Assert.InRange(snapshot.HousingSupportIndex, 0.45m, 1.60m);
        Assert.Equal(asOfUtc, snapshot.EvaluatedAtUtc);
    }

    private static IReadOnlyCollection<CityBusiness> CreateHealthyBusinesses(Guid cityId)
    {
        CityBusiness service = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, "Service", 2_000m);
        service.InjectCapital(Money.FromDecimal(1_000m));
        CityBusiness landlord = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Landlord, "Landlord", 2_000m);
        landlord.InjectCapital(Money.FromDecimal(1_000m));
        CityBusiness municipalVendor = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, "Vendor", 2_000m);
        municipalVendor.InjectCapital(Money.FromDecimal(1_000m));

        return [service, landlord, municipalVendor];
    }

    private static IReadOnlyCollection<CityBusiness> CreateStressedBusinesses(Guid cityId)
    {
        CityBusiness service = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, "Service", 100m);
        service.RecordOperatingExpense(Money.FromDecimal(180m));
        CityBusiness landlord = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Landlord, "Landlord", 100m);
        landlord.RecordOperatingExpense(Money.FromDecimal(170m));
        CityBusiness municipalVendor = EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, "Vendor", 100m);
        municipalVendor.RecordOperatingExpense(Money.FromDecimal(190m));

        return [service, landlord, municipalVendor];
    }
}
