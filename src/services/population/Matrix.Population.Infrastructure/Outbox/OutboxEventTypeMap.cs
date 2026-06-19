using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox;

namespace Matrix.Population.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [ClassicCityOutboxEventTypes.CityEconomyDailySettlementV1] = typeof(CityEconomyDailySettlementV1),
                [ClassicCityOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1] =
                    typeof(ClassicCityHouseholdAccountSyncBatchV1),
                [ClassicCityOutboxEventTypes.ClassicCityWorkplaceBusinessSyncBatchV1] =
                    typeof(ClassicCityWorkplaceBusinessSyncBatchV1),
                [ClassicCityOutboxEventTypes.ClassicCityWorkplacePayrollSettlementBatchV1] =
                    typeof(ClassicCityWorkplacePayrollSettlementBatchV1),
                [ClassicCityOutboxEventTypes.ClassicCityHouseholdCashflowSettlementBatchV1] =
                    typeof(ClassicCityHouseholdCashflowSettlementBatchV1)
            };
    }
}
