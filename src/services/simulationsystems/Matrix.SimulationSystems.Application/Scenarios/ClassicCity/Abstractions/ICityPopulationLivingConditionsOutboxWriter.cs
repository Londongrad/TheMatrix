using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationLivingConditionsOutboxWriter
    {
        Task AddClassicCityLivingConditionsSnapshotAsync(
            ClassicCityLivingConditionsSnapshotV1 snapshot,
            CancellationToken cancellationToken = default);
    }
}
