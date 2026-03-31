using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;

namespace Matrix.SimulationSystems.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                [SimulationSystemsOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1] =
                    typeof(ClassicCityOperationalExpenseIncurredV1),
                [SimulationSystemsOutboxEventTypes.ClassicCityLivingConditionsSnapshotV1] =
                    typeof(ClassicCityLivingConditionsSnapshotV1),
                [SimulationSystemsOutboxEventTypes.ClassicCitySystemsResourceDemandSnapshotV1] =
                    typeof(ClassicCitySystemsResourceDemandSnapshotV1)
            };
    }
}
