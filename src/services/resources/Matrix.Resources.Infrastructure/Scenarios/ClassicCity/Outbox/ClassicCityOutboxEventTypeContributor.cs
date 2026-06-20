using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.Resources.Infrastructure.Outbox;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxEventTypeContributor : IOutboxEventTypeContributor
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get; } =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [ClassicCityOutboxEventTypes.ClassicCityStockpileSnapshotV1] =
                    typeof(ClassicCityStockpileSnapshotV1),
                [ClassicCityOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1] =
                    typeof(ClassicCityOperationalExpenseIncurredV1)
            };
    }
}
