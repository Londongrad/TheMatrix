using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.GetCityBudgetSummary
{
    public sealed class GetCityBudgetSummaryQueryHandler(ICityBudgetRepository budgetRepository)
        : IRequestHandler<GetCityBudgetSummaryQuery, BudgetSummaryDto>
    {
        public async Task<BudgetSummaryDto> Handle(GetCityBudgetSummaryQuery request, CancellationToken cancellationToken)
        {
            CityBudget budget = await budgetRepository.GetByCityAsync(request.CityId, cancellationToken)
                ?? new CityBudget(CityBudgetId.New(), request.CityId);

            return new BudgetSummaryDto(
                Balance: budget.Balance,
                TotalTaxIncome: budget.TotalTaxIncome,
                TotalIncomeTaxIncome: budget.TotalIncomeTaxIncome,
                TotalSalesTaxIncome: budget.TotalSalesTaxIncome,
                TotalCityExpenses: budget.TotalCityExpenses,
                TotalRetailTurnover: budget.TotalRetailTurnover,
                TotalGrossPayroll: budget.TotalGrossPayroll,
                TotalNetPayroll: budget.TotalNetPayroll);
        }
    }
}
