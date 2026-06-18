using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationLivingConditionsOutboxWriter
    {
        Task AddClassicCityLivingConditionsSnapshotAsync(
            ClassicCityLivingConditionsSnapshotV1 snapshot,
            CancellationToken cancellationToken = default);
    }
}
