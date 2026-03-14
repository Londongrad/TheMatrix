using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;

namespace Matrix.Population.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [PopulationOutboxEventTypes.CityEconomyDailySettlementV1] = typeof(CityEconomyDailySettlementV1),
                [PopulationOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1] = typeof(ClassicCityHouseholdAccountSyncBatchV1),
                [PopulationOutboxEventTypes.ClassicCityWorkplaceBusinessSyncBatchV1] = typeof(ClassicCityWorkplaceBusinessSyncBatchV1),
                [PopulationOutboxEventTypes.ClassicCityWorkplacePayrollSettlementBatchV1] = typeof(ClassicCityWorkplacePayrollSettlementBatchV1),
                [PopulationOutboxEventTypes.ClassicCityHouseholdCashflowSettlementBatchV1] = typeof(ClassicCityHouseholdCashflowSettlementBatchV1)
            };
    }
}
