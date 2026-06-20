using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Matrix.Economy.Infrastructure.Outbox;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxEventTypeContributor : IOutboxEventTypeContributor
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get; } =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [ClassicCityOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1] =
                    typeof(ClassicCityOperationalBudgetPressureSnapshotV1),
                [ClassicCityOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1] =
                    typeof(ClassicCityCostOfLivingSnapshotV1),
                [ClassicCityOutboxEventTypes.ClassicCityServiceQualitySnapshotV1] =
                    typeof(ClassicCityServiceQualitySnapshotV1),
                [ClassicCityOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1] =
                    typeof(ClassicCityEmployerFinancialStressBatchV1),
                [ClassicCityOutboxEventTypes.ClassicCityHouseholdFinancialStressBatchV1] =
                    typeof(ClassicCityHouseholdFinancialStressBatchV1)
            };
    }
}
