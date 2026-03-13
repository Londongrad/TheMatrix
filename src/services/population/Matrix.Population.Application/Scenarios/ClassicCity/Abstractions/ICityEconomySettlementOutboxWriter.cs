using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;

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
    }
}
