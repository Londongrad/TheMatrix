using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICitySystemsResourceDemandOutboxWriter
    {
        Task AddClassicCitySystemsResourceDemandAsync(
            ClassicCitySystemsResourceDemandSnapshotV1 snapshot,
            CancellationToken cancellationToken = default);
    }
}
