using MediatR;

namespace Matrix.Economy.Application.UseCases.GetBudgetSummary
{
    public sealed record GetBudgetSummaryQuery : IRequest<BudgetSummaryDto>;
}
