using Matrix.Economy.Application.Abstractions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure
{
    public sealed class GetCityOperationalBudgetPressureQueryHandler(
        ICityOperationalBudgetPressureProjectionService projectionService)
        : IRequestHandler<GetCityOperationalBudgetPressureQuery, CityOperationalBudgetPressureDto>
    {
        public Task<CityOperationalBudgetPressureDto> Handle(
            GetCityOperationalBudgetPressureQuery request,
            CancellationToken cancellationToken)
        {
            return projectionService.GetAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);
        }
    }
}
