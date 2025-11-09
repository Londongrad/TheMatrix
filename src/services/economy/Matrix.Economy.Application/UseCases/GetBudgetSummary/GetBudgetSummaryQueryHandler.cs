using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.GetBudgetSummary
{
    public sealed class GetBudgetSummaryQueryHandler
    : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryDto>
    {
        private readonly ICityBudgetRepository _budgetRepository;

        public GetBudgetSummaryQueryHandler(ICityBudgetRepository budgetRepository)
        {
            _budgetRepository = budgetRepository;
        }

        public async Task<BudgetSummaryDto> Handle(GetBudgetSummaryQuery request, CancellationToken cancellationToken)
        {
            var budget = await _budgetRepository.GetCurrentAsync(cancellationToken);

            budget ??= new CityBudget(CityBudgetId.New());

            return new BudgetSummaryDto(
                Balance: budget.Balance,
                TotalTaxIncome: budget.TotalTaxIncome);
        }
    }
}
