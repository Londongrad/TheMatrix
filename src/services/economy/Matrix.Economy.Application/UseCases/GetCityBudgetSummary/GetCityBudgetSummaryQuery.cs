using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using MediatR;

namespace Matrix.Economy.Application.UseCases.GetCityBudgetSummary
{
    public sealed record GetCityBudgetSummaryQuery(Guid CityId) : IRequest<BudgetSummaryDto>;
}
