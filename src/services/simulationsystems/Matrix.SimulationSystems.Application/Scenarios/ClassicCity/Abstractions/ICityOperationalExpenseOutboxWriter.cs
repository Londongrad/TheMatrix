using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityOperationalExpenseOutboxWriter
    {
        Task AddClassicCityOperationalExpenseAsync(
            ClassicCityOperationalExpenseIncurredV1 expense,
            CancellationToken cancellationToken = default);
    }
}
