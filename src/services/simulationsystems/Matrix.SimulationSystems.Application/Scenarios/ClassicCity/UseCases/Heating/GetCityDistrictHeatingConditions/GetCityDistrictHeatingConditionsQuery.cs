using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions
{
    public sealed record GetCityDistrictHeatingConditionsQuery(Guid CityId)
        : IRequest<CityDistrictHeatingConditionsDto?>;
}
