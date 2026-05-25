using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates
{
    public sealed class CityBudgetTests
    {
        [Fact]
        public void Constructor_WhenUnitProfileIsProvided_NormalizesCodeAndInitializesZeros()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var budget = new CityBudget(
                id: new CityBudgetId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
                cityId: cityId,
                unitProfile: new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Currency,
                    Code: " cr ",
                    DisplayName: " Credits ",
                    Symbol: " ₡ "));

            Assert.Equal(
                expected: cityId,
                actual: budget.CityId);
            Assert.Equal(
                expected: CityBudgetUnitKind.Currency,
                actual: budget.UnitKind);
            Assert.Equal(
                expected: "CR",
                actual: budget.UnitCode);
            Assert.Equal(
                expected: "Credits",
                actual: budget.UnitDisplayName);
            Assert.Equal(
                expected: "₡",
                actual: budget.UnitSymbol);
            Assert.Equal(
                expected: Money.Zero,
                actual: budget.Balance);
            Assert.Equal(
                expected: Money.Zero,
                actual: budget.TotalTaxIncome);
            Assert.Equal(
                expected: Money.Zero,
                actual: budget.TotalCityExpenses);
        }

        [Fact]
        public void ApplySettlement_WhenCityMatches_UpdatesBalanceAndTotals()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            CityBudget budget = CreateBudget(cityId);
            CityBudgetSettlement settlement = CreateSettlement(cityId);
            var operatingExpense = new CityBudgetOperatingExpenseProfile(Money.FromDecimal(150m));

            budget.ApplySettlement(
                settlement: settlement,
                operatingExpense: operatingExpense);

            Assert.Equal(
                expected: Money.FromDecimal(-20m),
                actual: budget.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(130m),
                actual: budget.TotalTaxIncome);
            Assert.Equal(
                expected: Money.FromDecimal(80m),
                actual: budget.TotalIncomeTaxIncome);
            Assert.Equal(
                expected: Money.FromDecimal(50m),
                actual: budget.TotalSalesTaxIncome);
            Assert.Equal(
                expected: Money.FromDecimal(150m),
                actual: budget.TotalCityExpenses);
            Assert.Equal(
                expected: settlement.RetailTurnover,
                actual: budget.TotalRetailTurnover);
            Assert.Equal(
                expected: settlement.GrossPayroll,
                actual: budget.TotalGrossPayroll);
            Assert.Equal(
                expected: settlement.NetPayroll,
                actual: budget.TotalNetPayroll);
        }

        [Fact]
        public void ApplySettlement_WhenCityDoesNotMatch_ThrowsInvalidOperationException()
        {
            CityBudget budget = CreateBudget(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            CityBudgetSettlement settlement = CreateSettlement(Guid.Parse("33333333-3333-3333-3333-333333333333"));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => budget.ApplySettlement(
                settlement: settlement,
                operatingExpense: new CityBudgetOperatingExpenseProfile(Money.FromDecimal(150m))));

            Assert.Equal(
                expected: "Settlement city does not match budget city.",
                actual: exception.Message);
        }

        [Fact]
        public void ApplyLedgerEntry_WhenRevenueAndExpenseAreApplied_UpdatesTotalsAndBalance()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            CityBudget budget = CreateBudget(cityId);
            CityBudgetLedgerEntry revenue = CreateLedgerEntry(
                cityId: cityId,
                kind: CityBudgetLedgerEntryKind.Revenue,
                amount: 200m);
            CityBudgetLedgerEntry expense = CreateLedgerEntry(
                cityId: cityId,
                kind: CityBudgetLedgerEntryKind.Expense,
                amount: 75m);

            budget.ApplyLedgerEntry(revenue);
            budget.ApplyLedgerEntry(expense);

            Assert.Equal(
                expected: Money.FromDecimal(125m),
                actual: budget.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(200m),
                actual: budget.TotalDirectRevenue);
            Assert.Equal(
                expected: Money.FromDecimal(75m),
                actual: budget.TotalCityExpenses);
        }

        [Fact]
        public void ApplyLedgerEntry_WhenCityDoesNotMatch_ThrowsInvalidOperationException()
        {
            CityBudget budget = CreateBudget(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            CityBudgetLedgerEntry entry = CreateLedgerEntry(
                cityId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                kind: CityBudgetLedgerEntryKind.Revenue,
                amount: 200m);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => budget.ApplyLedgerEntry(entry));

            Assert.Equal(
                expected: "Ledger entry city does not match budget city.",
                actual: exception.Message);
        }

        [Fact]
        public void EnsureCompatibleUnit_WhenProfileMatches_IgnoresCodeCase()
        {
            CityBudget budget = CreateBudget(Guid.NewGuid());

            budget.EnsureCompatibleUnit(
                new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Currency,
                    Code: "cr",
                    DisplayName: "Credits",
                    Symbol: "₡"));
        }

        [Fact]
        public void EnsureCompatibleUnit_WhenProfileDiffers_ThrowsInvalidOperationException()
        {
            CityBudget budget = CreateBudget(Guid.NewGuid());

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => budget.EnsureCompatibleUnit(
                    new CityBudgetUnitProfile(
                        Kind: CityBudgetUnitKind.Resource,
                        Code: "res",
                        DisplayName: "Resources",
                        Symbol: "R")));

            Assert.Contains(
                expectedSubstring: "Budget unit mismatch.",
                actualString: exception.Message);
        }

        private static CityBudget CreateBudget(Guid cityId)
        {
            return new CityBudget(
                id: new CityBudgetId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
                cityId: cityId,
                unitProfile: new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Currency,
                    Code: "CR",
                    DisplayName: "Credits",
                    Symbol: "₡"));
        }

        private static CityBudgetSettlement CreateSettlement(Guid cityId)
        {
            return new CityBudgetSettlement(
                id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                cityId: cityId,
                tickId: 10,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 2,
                    day: 3),
                settledDays: 2,
                householdCount: 12,
                residentCount: 40,
                grossPayroll: Money.FromDecimal(800m),
                incomeTax: Money.FromDecimal(80m),
                netPayroll: Money.FromDecimal(720m),
                retailTurnover: Money.FromDecimal(1000m),
                retailTax: Money.FromDecimal(50m),
                housingSpend: Money.FromDecimal(500m),
                correlationId: "budget-cycle-1",
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero));
        }

        private static CityBudgetLedgerEntry CreateLedgerEntry(
            Guid cityId,
            CityBudgetLedgerEntryKind kind,
            decimal amount)
        {
            return new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: cityId,
                occurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 6,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                kind: kind,
                category: CityBudgetCategory.General,
                amount: Money.FromDecimal(amount),
                title: kind.ToString(),
                description: null,
                source: CityBudgetLedgerEntrySource.Manual,
                referenceCode: "ledger-1");
        }
    }
}
