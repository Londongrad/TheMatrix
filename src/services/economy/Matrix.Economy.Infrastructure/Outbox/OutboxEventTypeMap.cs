using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;

namespace Matrix.Economy.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [EconomyOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1] =
                    typeof(ClassicCityOperationalBudgetPressureSnapshotV1),
                [EconomyOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1] =
                    typeof(ClassicCityCostOfLivingSnapshotV1),
                [EconomyOutboxEventTypes.ClassicCityServiceQualitySnapshotV1] =
                    typeof(ClassicCityServiceQualitySnapshotV1),
                [EconomyOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1] =
                    typeof(ClassicCityEmployerFinancialStressBatchV1),
                [EconomyOutboxEventTypes.ClassicCityHouseholdFinancialStressBatchV1] =
                    typeof(ClassicCityHouseholdFinancialStressBatchV1)
            };
    }
}
