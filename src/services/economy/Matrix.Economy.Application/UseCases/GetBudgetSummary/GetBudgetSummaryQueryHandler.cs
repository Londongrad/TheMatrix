using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.GetBudgetSummary
{
    public sealed class GetBudgetSummaryQueryHandler(ICityBudgetRepository budgetRepository)
        : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryDto>
    {
        public async Task<BudgetSummaryDto> Handle(GetBudgetSummaryQuery request, CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBudget> budgets = await budgetRepository.ListAsync(cancellationToken);
            (string unitKind, string unitCode, string unitDisplayName, string unitSymbol) = ResolveUnitDescriptor(budgets);

            return new BudgetSummaryDto(
                UnitKind: unitKind,
                UnitCode: unitCode,
                UnitDisplayName: unitDisplayName,
                UnitSymbol: unitSymbol,
                Balance: Sum(budgets, x => x.Balance),
                TotalTaxIncome: Sum(budgets, x => x.TotalTaxIncome),
                TotalIncomeTaxIncome: Sum(budgets, x => x.TotalIncomeTaxIncome),
                TotalSalesTaxIncome: Sum(budgets, x => x.TotalSalesTaxIncome),
                TotalDirectRevenue: Sum(budgets, x => x.TotalDirectRevenue),
                TotalCityExpenses: Sum(budgets, x => x.TotalCityExpenses),
                TotalRetailTurnover: Sum(budgets, x => x.TotalRetailTurnover),
                TotalGrossPayroll: Sum(budgets, x => x.TotalGrossPayroll),
                TotalNetPayroll: Sum(budgets, x => x.TotalNetPayroll));
        }

        private static Money Sum(
            IEnumerable<CityBudget> budgets,
            Func<CityBudget, Money> selector)
        {
            return budgets.Aggregate(Money.Zero, (current, budget) => current.Add(selector(budget)));
        }

        private static (string unitKind, string unitCode, string unitDisplayName, string unitSymbol) ResolveUnitDescriptor(
            IReadOnlyList<CityBudget> budgets)
        {
            if (budgets.Count == 0)
            {
                return ("Currency", "MNY", "Money", "¤");
            }

            CityBudget first = budgets[0];
            bool isMixed = budgets.Any(x =>
                !string.Equals(x.UnitCode, first.UnitCode, StringComparison.OrdinalIgnoreCase)
                || x.UnitKind != first.UnitKind
                || !string.Equals(x.UnitDisplayName, first.UnitDisplayName, StringComparison.Ordinal)
                || !string.Equals(x.UnitSymbol, first.UnitSymbol, StringComparison.Ordinal));

            return isMixed
                ? ("Mixed", "MIXED", "Mixed units", "∑")
                : (first.UnitKind.ToString(), first.UnitCode, first.UnitDisplayName, first.UnitSymbol);
        }
    }
}
