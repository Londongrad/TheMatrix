using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityOperationalExpenseOutboxWriter
    {
        Task AddClassicCityOperationalExpenseAsync(
            ClassicCityOperationalExpenseIncurredV1 expense,
            CancellationToken cancellationToken = default);
    }
}
