using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityDistrictUtilityIncidentConditions
{
    public sealed record GetCityDistrictUtilityIncidentConditionsQuery(Guid CityId)
        : IRequest<CityDistrictUtilityIncidentConditionsDto?>;
}
