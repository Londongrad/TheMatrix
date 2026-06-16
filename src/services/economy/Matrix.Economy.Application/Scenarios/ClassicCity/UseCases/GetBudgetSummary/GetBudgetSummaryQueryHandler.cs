using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetBudgetSummary
{
    public sealed class GetBudgetSummaryQueryHandler(ICityBudgetRepository budgetRepository)
        : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryDto>
    {
        public async Task<BudgetSummaryDto> Handle(
            GetBudgetSummaryQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBudget> budgets = await budgetRepository.ListAsync(cancellationToken);
            (string unitKind, string unitCode, string unitDisplayName, string unitSymbol) =
                ResolveUnitDescriptor(budgets);

            return new BudgetSummaryDto(
                UnitKind: unitKind,
                UnitCode: unitCode,
                UnitDisplayName: unitDisplayName,
                UnitSymbol: unitSymbol,
                Balance: Sum(
                    budgets: budgets,
                    selector: x => x.Balance),
                TotalTaxIncome: Sum(
                    budgets: budgets,
                    selector: x => x.TotalTaxIncome),
                TotalIncomeTaxIncome: Sum(
                    budgets: budgets,
                    selector: x => x.TotalIncomeTaxIncome),
                TotalSalesTaxIncome: Sum(
                    budgets: budgets,
                    selector: x => x.TotalSalesTaxIncome),
                TotalDirectRevenue: Sum(
                    budgets: budgets,
                    selector: x => x.TotalDirectRevenue),
                TotalCityExpenses: Sum(
                    budgets: budgets,
                    selector: x => x.TotalCityExpenses),
                TotalRetailTurnover: Sum(
                    budgets: budgets,
                    selector: x => x.TotalRetailTurnover),
                TotalGrossPayroll: Sum(
                    budgets: budgets,
                    selector: x => x.TotalGrossPayroll),
                TotalNetPayroll: Sum(
                    budgets: budgets,
                    selector: x => x.TotalNetPayroll));
        }

        private static Money Sum(
            IEnumerable<CityBudget> budgets,
            Func<CityBudget, Money> selector)
        {
            return budgets.Aggregate(
                seed: Money.Zero,
                func: (
                    current,
                    budget) => current.Add(selector(budget)));
        }

        private static (string unitKind, string unitCode, string unitDisplayName, string unitSymbol)
            ResolveUnitDescriptor(IReadOnlyList<CityBudget> budgets)
        {
            if (budgets.Count == 0)
                return ("Currency", "MNY", "Money", "¤");

            CityBudget first = budgets[0];
            bool isMixed = budgets.Any(x =>
                !string.Equals(
                    a: x.UnitCode,
                    b: first.UnitCode,
                    comparisonType: StringComparison.OrdinalIgnoreCase) ||
                x.UnitKind != first.UnitKind ||
                !string.Equals(
                    a: x.UnitDisplayName,
                    b: first.UnitDisplayName,
                    comparisonType: StringComparison.Ordinal) ||
                !string.Equals(
                    a: x.UnitSymbol,
                    b: first.UnitSymbol,
                    comparisonType: StringComparison.Ordinal));

            return isMixed
                ? ("Mixed", "MIXED", "Mixed units", "∑")
                : (first.UnitKind.ToString(), first.UnitCode, first.UnitDisplayName, first.UnitSymbol);
        }
    }
}
