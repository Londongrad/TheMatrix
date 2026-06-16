using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityBudgetSummary;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.GetCityBudgetSummary
{
    public sealed class GetCityBudgetSummaryQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityBudgetExists_ReturnsBudgetSummary()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBudget budget = CreateBudget(cityId);
            budget.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityId,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    amount: 150m,
                    title: "Grant"));
            budget.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityId,
                    kind: CityBudgetLedgerEntryKind.Expense,
                    amount: 45m,
                    title: "Ops"));
            var budgetRepository = new FakeCityBudgetRepository
            {
                BudgetByCity = budget
            };
            var handler = new GetCityBudgetSummaryQueryHandler(budgetRepository);

            BudgetSummaryDto result = await handler.Handle(
                request: new GetCityBudgetSummaryQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: budgetRepository.RequestedCityId);
            Assert.Equal(
                expected: "Currency",
                actual: result.UnitKind);
            Assert.Equal(
                expected: Money.FromDecimal(105m),
                actual: result.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(150m),
                actual: result.TotalDirectRevenue);
            Assert.Equal(
                expected: Money.FromDecimal(45m),
                actual: result.TotalCityExpenses);
        }

        [Fact]
        public async Task Handle_WhenCityBudgetMissing_ReturnsZeroSummary()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var budgetRepository = new FakeCityBudgetRepository();
            var handler = new GetCityBudgetSummaryQueryHandler(budgetRepository);

            BudgetSummaryDto result = await handler.Handle(
                request: new GetCityBudgetSummaryQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: budgetRepository.RequestedCityId);
            Assert.Equal(
                expected: "Currency",
                actual: result.UnitKind);
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
    }
}
