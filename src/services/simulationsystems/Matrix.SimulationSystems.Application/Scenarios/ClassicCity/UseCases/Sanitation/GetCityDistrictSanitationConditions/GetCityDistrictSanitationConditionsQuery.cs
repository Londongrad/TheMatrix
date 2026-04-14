using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions
{
    public sealed record GetCityDistrictSanitationConditionsQuery(Guid CityId)
        : IRequest<CityDistrictSanitationConditionsDto?>;
}
