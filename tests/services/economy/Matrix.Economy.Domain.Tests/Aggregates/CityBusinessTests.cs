using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates
{
    public sealed class CityBusinessTests
    {
        [Fact]
        public void Constructor_WhenArgumentsAreValid_NormalizesFieldsAndInitializesTotals()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                kind: CityBusinessKind.Service,
                name: " Central Services ",
                externalReferenceCode: " ext-1 ",
                templateKey: " service-template ");

            Assert.Equal(
                expected: cityId,
                actual: business.CityId);
            Assert.Equal(
                expected: "Central Services",
                actual: business.Name);
            Assert.Equal(
                expected: "ext-1",
                actual: business.ExternalReferenceCode);
            Assert.Equal(
                expected: "service-template",
                actual: business.TemplateKey);
            Assert.Equal(
                expected: CityBudgetUnitKind.Currency,
                actual: business.UnitKind);
            Assert.Equal(
                expected: "CR",
                actual: business.UnitCode);
            Assert.Equal(
                expected: "Credits",
                actual: business.UnitDisplayName);
            Assert.Equal(
                expected: "₡",
                actual: business.UnitSymbol);
            Assert.Equal(
                expected: Money.FromDecimal(1000m),
                actual: business.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(1000m),
                actual: business.TotalCapitalInjections);
            Assert.Equal(
                expected: Money.Zero,
                actual: business.TaxReserve);
            Assert.Equal(
                expected: Money.Zero,
                actual: business.TotalRetailTurnover);
        }

        [Fact]
        public void EnsureCanIssuePayroll_WhenKindDoesNotSupportPayroll_ThrowsInvalidOperationException()
        {
            CityBusiness business = CreateBusiness(
                cityId: Guid.NewGuid(),
                kind: CityBusinessKind.RetailStore);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => business.EnsureCanIssuePayroll());

            Assert.Equal(
                expected: "Business kind 'RetailStore' cannot issue payroll.",
                actual: exception.Message);
        }

        [Fact]
        public void RecordRetailSale_WhenArgumentsAreValid_UpdatesBalanceReserveAndTotals()
        {
            CityBusiness business = CreateBusiness(
                cityId: Guid.NewGuid(),
                kind: CityBusinessKind.RetailStore);

            business.RecordRetailSale(
                grossAmount: Money.FromDecimal(120m),
                salesTaxAmount: Money.FromDecimal(20m));

            Assert.Equal(
                expected: Money.FromDecimal(1120m),
                actual: business.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(20m),
                actual: business.TaxReserve);
            Assert.Equal(
                expected: Money.FromDecimal(120m),
                actual: business.TotalRetailTurnover);
            Assert.Equal(
                expected: Money.FromDecimal(100m),
                actual: business.TotalNetSalesRevenue);
        }

        [Fact]
        public void SettlePayroll_WhenBalanceIsInsufficient_ReturnsPartialOutcomeAndSubtractsPaidAmount()
        {
            CityBusiness business = CreateBusiness(
                cityId: Guid.NewGuid(),
                kind: CityBusinessKind.Service,
                initialCapital: 150m);

            CityBusinessPayrollSettlementOutcome outcome = business.SettlePayroll(
                requestedGrossPayroll: Money.FromDecimal(200m),
                requestedIncomeTax: Money.FromDecimal(40m));

            Assert.True(outcome.IsPartiallyPaid);
            Assert.False(outcome.IsFullyPaid);
            Assert.False(outcome.IsMissed);
            Assert.Equal(
                expected: Money.FromDecimal(150m),
                actual: outcome.PaidGrossPayroll);
            Assert.Equal(
                expected: Money.FromDecimal(30m),
                actual: outcome.PaidIncomeTax);
            Assert.Equal(
                expected: Money.FromDecimal(120m),
                actual: outcome.PaidNetPayroll);
            Assert.Equal(
                expected: Money.FromDecimal(50m),
                actual: outcome.GrossShortfall);
            Assert.Equal(
                expected: 0.75m,
                actual: outcome.FulfillmentRatio);
            Assert.Equal(
                expected: Money.Zero,
                actual: business.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(150m),
                actual: business.TotalOperatingExpenses);
        }

        [Fact]
        public void RemitTax_WhenAmountExceedsReserve_ThrowsInvalidOperationException()
        {
            CityBusiness business = CreateBusiness(
                cityId: Guid.NewGuid(),
                kind: CityBusinessKind.RetailStore);
            business.RecordRetailSale(
                grossAmount: Money.FromDecimal(120m),
                salesTaxAmount: Money.FromDecimal(20m));

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => business.RemitTax(Money.FromDecimal(25m)));

            Assert.Equal(
                expected: "Cannot remit more tax than the current reserve.",
                actual: exception.Message);
        }

        [Fact]
        public void RemitTax_WhenAmountIsWithinReserve_UpdatesBalanceReserveAndTotals()
        {
            CityBusiness business = CreateBusiness(
                cityId: Guid.NewGuid(),
                kind: CityBusinessKind.RetailStore);
            business.RecordRetailSale(
                grossAmount: Money.FromDecimal(120m),
                salesTaxAmount: Money.FromDecimal(20m));

            business.RemitTax(Money.FromDecimal(15m));

            Assert.Equal(
                expected: Money.FromDecimal(1105m),
                actual: business.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(5m),
                actual: business.TaxReserve);
            Assert.Equal(
                expected: Money.FromDecimal(15m),
                actual: business.TotalTaxRemitted);
        }

        private static CityBusiness CreateBusiness(
            Guid cityId,
            CityBusinessKind kind,
            string name = "Business",
            string? externalReferenceCode = "external-1",
            string? templateKey = "template-1",
            decimal initialCapital = 1000m)
        {
            return new CityBusiness(
                id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                cityId: cityId,
                name: name,
                externalReferenceCode: externalReferenceCode,
                templateKey: templateKey,
                kind: kind,
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero),
                unitProfile: new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Currency,
                    Code: "CR",
                    DisplayName: "Credits",
                    Symbol: "₡"),
                initialCapital: Money.FromDecimal(initialCapital));
        }
    }
}
