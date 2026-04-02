using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions
{
    public sealed record GetCityRoadSegmentConditionsQuery(Guid CityId) : IRequest<CityRoadSegmentConditionsDto?>;
}
