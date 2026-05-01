using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Services;

public sealed class CityMunicipalOperatingCyclePolicyTests
{
    [Fact]
    public void BuildDisbursements_WhenAllocationHasNoAvailableAmount_ReturnsEmpty()
    {
        Guid cityId = Guid.NewGuid();
        var allocation = EconomyTestData.CreateAllocation(
            cityId: cityId,
            category: CityBudgetCategory.Operations,
            targetAmount: 100m,
            totalSpent: 100m);
        var policy = new CityMunicipalOperatingCyclePolicy();

        IReadOnlyList<Matrix.Economy.Domain.Models.CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
            allocation: allocation,
            businesses:
            [
                EconomyTestData.CreateBusiness(cityId, CityBusinessKind.Service, name: "Alpha Services")
            ]);

        Assert.Empty(decisions);
    }

    [Fact]
    public void BuildDisbursements_WhenCategoryIsTaxation_ReturnsEmptyEvenWithAvailableAmount()
    {
        Guid cityId = Guid.NewGuid();
        var allocation = EconomyTestData.CreateAllocation(
            cityId: cityId,
            category: CityBudgetCategory.Taxation,
            targetAmount: 500m);
        var policy = new CityMunicipalOperatingCyclePolicy();

        IReadOnlyList<Matrix.Economy.Domain.Models.CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
            allocation: allocation,
            businesses:
            [
                EconomyTestData.CreateBusiness(cityId, CityBusinessKind.MunicipalVendor, name: "Tax Office")
            ]);

        Assert.Empty(decisions);
    }

    [Fact]
    public void BuildDisbursements_WhenEligibleBusinessesExist_DistributesTenPercentEvenlyInNameOrder()
    {
        Guid cityId = Guid.NewGuid();
        var allocation = EconomyTestData.CreateAllocation(
            cityId: cityId,
            category: CityBudgetCategory.Operations,
            targetAmount: 1000m);
        Matrix.Economy.Domain.Aggregates.CityBusiness zebra = EconomyTestData.CreateBusiness(
            cityId: cityId,
            kind: CityBusinessKind.Service,
            name: "Zebra Services");
        Matrix.Economy.Domain.Aggregates.CityBusiness alpha = EconomyTestData.CreateBusiness(
            cityId: cityId,
            kind: CityBusinessKind.MunicipalVendor,
            name: "Alpha Vendor");
        Matrix.Economy.Domain.Aggregates.CityBusiness ignored = EconomyTestData.CreateBusiness(
            cityId: cityId,
            kind: CityBusinessKind.RetailStore,
            name: "Retail Shop");
        var policy = new CityMunicipalOperatingCyclePolicy();

        IReadOnlyList<Matrix.Economy.Domain.Models.CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
            allocation: allocation,
            businesses: [zebra, ignored, alpha]);

        Assert.Collection(
            decisions,
            first =>
            {
                Assert.Equal(alpha.Id, first.BusinessId);
                Assert.Equal(50m, first.Amount);
            },
            second =>
            {
                Assert.Equal(zebra.Id, second.BusinessId);
                Assert.Equal(50m, second.Amount);
            });
    }

    [Fact]
    public void BuildDisbursements_WhenPlannedCycleRoundsToZero_UsesMinimumCentFromAvailableAmount()
    {
        Guid cityId = Guid.NewGuid();
        var allocation = EconomyTestData.CreateAllocation(
            cityId: cityId,
            category: CityBudgetCategory.Healthcare,
            targetAmount: 0.04m);
        Matrix.Economy.Domain.Aggregates.CityBusiness service = EconomyTestData.CreateBusiness(
            cityId: cityId,
            kind: CityBusinessKind.Service,
            name: "Clinic Support");
        var policy = new CityMunicipalOperatingCyclePolicy();

        IReadOnlyList<Matrix.Economy.Domain.Models.CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
            allocation: allocation,
            businesses: [service]);

        Matrix.Economy.Domain.Models.CityMunicipalOperatingDisbursementDecision decision = Assert.Single(decisions);
        Assert.Equal(service.Id, decision.BusinessId);
        Assert.Equal(0.01m, decision.Amount);
    }
}
