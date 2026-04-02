namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions
{
    public interface ICityRoadSegmentConditionsClient
    {
        Task<CityRoadSegmentConditionsSnapshot?> GetByCityIdAsync(
            Guid cityId,
            CancellationToken cancellationToken);
    }
}
