using MediatR;

namespace Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure
{
    public sealed record GetCityOperationalBudgetPressureQuery(Guid CityId)
        : IRequest<CityOperationalBudgetPressureDto>;
}
