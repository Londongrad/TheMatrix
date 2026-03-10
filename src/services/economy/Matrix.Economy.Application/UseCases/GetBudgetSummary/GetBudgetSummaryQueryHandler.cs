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

            return new BudgetSummaryDto(
                Balance: Sum(budgets, x => x.Balance),
                TotalTaxIncome: Sum(budgets, x => x.TotalTaxIncome),
                TotalIncomeTaxIncome: Sum(budgets, x => x.TotalIncomeTaxIncome),
                TotalSalesTaxIncome: Sum(budgets, x => x.TotalSalesTaxIncome),
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
    }
}
