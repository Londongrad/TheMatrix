using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions
{
    public interface IClassicCityRoutePlanner
    {
        CityRouteDto Plan(
            Guid cityId,
            string profile,
            CityRoutePointDto from,
            CityRoutePointDto to,
            IReadOnlyList<RoadNode> roadNodes,
            IReadOnlyList<RoadSegment> roadSegments,
            CityRoadSegmentConditionsSnapshot? segmentConditions);
    }
}
