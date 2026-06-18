using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityOperationalExpenseOutboxWriter
    {
        Task AddClassicCityOperationalExpenseAsync(
            ClassicCityOperationalExpenseIncurredV1 expense,
            CancellationToken cancellationToken = default);
    }
}
