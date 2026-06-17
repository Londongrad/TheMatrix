using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Services
{
    public sealed class CityMunicipalOperatingCyclePolicyTests
    {
        [Fact]
        public void BuildDisbursements_WhenAllocationHasNoAvailableAmount_ReturnsEmpty()
        {
            var cityId = Guid.NewGuid();
            CityBudgetAllocation allocation = EconomyTestData.CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Operations,
                targetAmount: 100m,
                totalSpent: 100m);
            var policy = new CityMunicipalOperatingCyclePolicy();

            IReadOnlyList<CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
                allocation: allocation,
                businesses:
                [
                    EconomyTestData.CreateBusiness(
                        cityId: cityId,
                        kind: CityBusinessKind.Service,
                        name: "Alpha Services")
                ]);

            Assert.Empty(decisions);
        }

        [Fact]
        public void BuildDisbursements_WhenCategoryIsTaxation_ReturnsEmptyEvenWithAvailableAmount()
        {
            var cityId = Guid.NewGuid();
            CityBudgetAllocation allocation = EconomyTestData.CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Taxation,
                targetAmount: 500m);
            var policy = new CityMunicipalOperatingCyclePolicy();

            IReadOnlyList<CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
                allocation: allocation,
                businesses:
                [
                    EconomyTestData.CreateBusiness(
                        cityId: cityId,
                        kind: CityBusinessKind.MunicipalVendor,
                        name: "Tax Office")
                ]);

            Assert.Empty(decisions);
        }

        [Fact]
        public void BuildDisbursements_WhenEligibleBusinessesExist_DistributesTenPercentEvenlyInNameOrder()
        {
            var cityId = Guid.NewGuid();
            CityBudgetAllocation allocation = EconomyTestData.CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Operations,
                targetAmount: 1000m);
            CityBusiness zebra = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: "Zebra Services");
            CityBusiness alpha = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.MunicipalVendor,
                name: "Alpha Vendor");
            CityBusiness ignored = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.RetailStore,
                name: "Retail Shop");
            var policy = new CityMunicipalOperatingCyclePolicy();

            IReadOnlyList<CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
                allocation: allocation,
                businesses:
                [
                    zebra,
                    ignored,
                    alpha
                ]);

            Assert.Collection(
                collection: decisions,
                first =>
                {
                    Assert.Equal(
                        expected: alpha.Id,
                        actual: first.BusinessId);
                    Assert.Equal(
                        expected: 50m,
                        actual: first.Amount);
                },
                second =>
                {
                    Assert.Equal(
                        expected: zebra.Id,
                        actual: second.BusinessId);
                    Assert.Equal(
                        expected: 50m,
                        actual: second.Amount);
                });
        }

        [Fact]
        public void BuildDisbursements_WhenPlannedCycleRoundsToZero_UsesMinimumCentFromAvailableAmount()
        {
            var cityId = Guid.NewGuid();
            CityBudgetAllocation allocation = EconomyTestData.CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Healthcare,
                targetAmount: 0.04m);
            CityBusiness service = EconomyTestData.CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: "Clinic Support");
            var policy = new CityMunicipalOperatingCyclePolicy();

            IReadOnlyList<CityMunicipalOperatingDisbursementDecision> decisions = policy.BuildDisbursements(
                allocation: allocation,
                businesses: [service]);

            CityMunicipalOperatingDisbursementDecision decision = Assert.Single(decisions);
            Assert.Equal(
                expected: service.Id,
                actual: decision.BusinessId);
            Assert.Equal(
                expected: 0.01m,
                actual: decision.Amount);
        }
    }
}
