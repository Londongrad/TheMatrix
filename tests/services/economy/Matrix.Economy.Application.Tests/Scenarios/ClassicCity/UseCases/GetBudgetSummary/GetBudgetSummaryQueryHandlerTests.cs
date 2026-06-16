using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetBudgetSummary;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.GetBudgetSummary
{
    public sealed class GetBudgetSummaryQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenNoBudgets_ReturnsDefaultSummary()
        {
            var budgetRepository = new FakeCityBudgetRepository();
            var handler = new GetBudgetSummaryQueryHandler(budgetRepository);

            BudgetSummaryDto result = await handler.Handle(
                request: new GetBudgetSummaryQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Currency",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "MNY",
                actual: result.UnitCode);
            Assert.Equal(
                expected: "Money",
                actual: result.UnitDisplayName);
            Assert.Equal(
                expected: Money.Zero,
                actual: result.Balance);
            Assert.Equal(
                expected: Money.Zero,
                actual: result.TotalDirectRevenue);
            Assert.Equal(
                expected: Money.Zero,
                actual: result.TotalCityExpenses);
        }

        [Fact]
        public async Task Handle_WhenBudgetsShareUnit_ReturnsAggregatedTotals()
        {
            var cityA = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var cityB = Guid.Parse("11111111-2222-3333-4444-555555555555");
            CityBudget first = CreateBudget(cityA);
            first.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityA,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    amount: 250m,
                    title: "Grant"));
            first.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityA,
                    kind: CityBudgetLedgerEntryKind.Expense,
                    amount: 40m,
                    title: "Maintenance"));

            CityBudget second = CreateBudget(cityB);
            second.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityB,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    amount: 100m,
                    title: "Tax"));
            second.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityB,
                    kind: CityBudgetLedgerEntryKind.Expense,
                    amount: 10m,
                    title: "Fuel"));

            var budgetRepository = new FakeCityBudgetRepository
            {
                Budgets =
                [
                    first,
                    second
                ]
            };
            var handler = new GetBudgetSummaryQueryHandler(budgetRepository);

            BudgetSummaryDto result = await handler.Handle(
                request: new GetBudgetSummaryQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Currency",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "MNY",
                actual: result.UnitCode);
            Assert.Equal(
                expected: Money.FromDecimal(350m),
                actual: result.TotalDirectRevenue);
            Assert.Equal(
                expected: Money.FromDecimal(50m),
                actual: result.TotalCityExpenses);
            Assert.Equal(
                expected: Money.FromDecimal(300m),
                actual: result.Balance);
        }

        [Fact]
        public async Task Handle_WhenBudgetsUseMixedUnits_ReturnsMixedDescriptor()
        {
            var cityA = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var cityB = Guid.Parse("11111111-2222-3333-4444-555555555555");
            CityBudget first = CreateBudget(cityA);
            CityBudget second = CreateBudget(
                cityId: cityB,
                unitProfile: new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Currency,
                    Code: "CRD",
                    DisplayName: "Credits",
                    Symbol: "CR"));
            var budgetRepository = new FakeCityBudgetRepository
            {
                Budgets =
                [
                    first,
                    second
                ]
            };
            var handler = new GetBudgetSummaryQueryHandler(budgetRepository);

            BudgetSummaryDto result = await handler.Handle(
                request: new GetBudgetSummaryQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Mixed",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "MIXED",
                actual: result.UnitCode);
            Assert.Equal(
                expected: "Mixed units",
                actual: result.UnitDisplayName);
        }
    }
}
