using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;

namespace Matrix.SimulationSystems.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                [SimulationSystemsOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1] =
                    typeof(ClassicCityOperationalExpenseIncurredV1)
            };
    }
}
