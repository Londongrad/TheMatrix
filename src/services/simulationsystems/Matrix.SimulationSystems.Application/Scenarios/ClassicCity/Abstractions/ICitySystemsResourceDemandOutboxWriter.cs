using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICitySystemsResourceDemandOutboxWriter
    {
        Task AddClassicCitySystemsResourceDemandAsync(
            ClassicCitySystemsResourceDemandSnapshotV1 snapshot,
            CancellationToken cancellationToken = default);
    }
}
