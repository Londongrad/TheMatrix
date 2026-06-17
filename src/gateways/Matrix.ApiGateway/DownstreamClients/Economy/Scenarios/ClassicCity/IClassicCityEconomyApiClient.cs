using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Requests;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Views;

namespace Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity
{
    public interface IClassicCityEconomyApiClient
    {
        Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default);

        Task<bool> HealthAsync(CancellationToken cancellationToken = default);

        Task<EconomySummaryView?> GetCitySummaryAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityOperationalBudgetPressureView?> GetCityOperationalBudgetPressureAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityBusinessView>> GetCityBusinessesAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityHouseholdAccountView>> GetCityHouseholdAccountsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CursorPagedResult<BudgetLedgerEntryView>> GetCityBudgetLedgerFeedAsync(
            Guid cityId,
            string? cursor = null,
            int pageSize = 50,
            CancellationToken cancellationToken = default);

        Task<CursorPagedResult<CityBusinessLedgerEntryView>> GetCityBusinessLedgerFeedAsync(
            Guid businessId,
            string? cursor = null,
            int pageSize = 50,
            CancellationToken cancellationToken = default);

        Task<CursorPagedResult<CityHouseholdAccountLedgerEntryView>> GetCityHouseholdAccountLedgerFeedAsync(
            Guid householdAccountId,
            string? cursor = null,
            int pageSize = 50,
            CancellationToken cancellationToken = default);

        Task<CityEconomyBootstrapResultView> InitializeCityEconomyAsync(
            Guid cityId,
            InitializeCityEconomyRequest request,
            CancellationToken cancellationToken = default);
    }
}
