using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityMapTopologyClient
    {
        Task<CityRoadGraphTopologyDto?> GetRoadGraphAsync(
            Guid cityId,
            CancellationToken cancellationToken);
    }
}
