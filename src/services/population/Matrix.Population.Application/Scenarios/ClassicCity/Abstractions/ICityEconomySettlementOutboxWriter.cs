using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityEconomySettlementOutboxWriter
    {
        Task AddCityDailySettlementAsync(
            CityEconomyDailySettlementV1 settlement,
            CancellationToken cancellationToken = default);

        Task AddClassicCityHouseholdAccountSyncBatchAsync(
            ClassicCityHouseholdAccountSyncBatchV1 batch,
            CancellationToken cancellationToken = default);

        Task AddClassicCityWorkplaceBusinessSyncBatchAsync(
            ClassicCityWorkplaceBusinessSyncBatchV1 batch,
            CancellationToken cancellationToken = default);

        Task AddClassicCityWorkplacePayrollSettlementBatchAsync(
            ClassicCityWorkplacePayrollSettlementBatchV1 batch,
            CancellationToken cancellationToken = default);

        Task AddClassicCityHouseholdCashflowSettlementBatchAsync(
            ClassicCityHouseholdCashflowSettlementBatchV1 batch,
            CancellationToken cancellationToken = default);
    }
}
