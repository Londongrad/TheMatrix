using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    public interface IEconomyApiClient
    {
        Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default);

        Task<EconomySummaryView?> GetCitySummaryAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityOperationalBudgetPressureView?> GetCityOperationalBudgetPressureAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityEconomyBootstrapResultView> InitializeCityEconomyAsync(
            Guid cityId,
            InitializeCityEconomyRequest request,
            CancellationToken cancellationToken = default);

        Task<bool> HealthAsync(CancellationToken cancellationToken = default);
    }
}
