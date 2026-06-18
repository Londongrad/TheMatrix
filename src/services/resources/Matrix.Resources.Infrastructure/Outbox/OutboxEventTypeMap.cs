using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;

namespace Matrix.Resources.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [ResourcesOutboxEventTypes.ClassicCityStockpileSnapshotV1] = typeof(ClassicCityStockpileSnapshotV1),
                [ResourcesOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1] =
                    typeof(ClassicCityOperationalExpenseIncurredV1)
            };
    }
}
