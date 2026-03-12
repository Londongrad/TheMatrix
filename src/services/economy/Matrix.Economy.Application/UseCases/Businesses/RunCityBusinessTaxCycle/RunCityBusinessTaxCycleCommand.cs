using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle
{
    public sealed record RunCityBusinessTaxCycleCommand(
        Guid CityId,
        CityBudgetCategory BudgetCategory) : IRequest<RunCityBusinessTaxCycleResultDto>;
}
