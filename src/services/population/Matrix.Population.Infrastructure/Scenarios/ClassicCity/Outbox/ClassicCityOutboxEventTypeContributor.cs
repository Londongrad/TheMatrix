using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.Population.Infrastructure.Outbox;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxEventTypeContributor : IOutboxEventTypeContributor
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get; } =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [ClassicCityOutboxEventTypes.ResidentActivityConditionsBatchV1] = typeof(ClassicCityResidentActivityConditionsBatchV1),
                [ClassicCityOutboxEventTypes.CityEconomyDailySettlementV1] =
                    typeof(CityEconomyDailySettlementV1),
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
