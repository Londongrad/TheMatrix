using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityStockpileSnapshotOutboxWriter
    {
        Task AddClassicCityStockpileSnapshotAsync(
            ClassicCityStockpileSnapshotV1 snapshot,
            CancellationToken cancellationToken = default);
    }
}
