using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;

namespace Matrix.Population.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [PopulationOutboxEventTypes.CityEconomyDailySettlementV1] = typeof(CityEconomyDailySettlementV1)
            };
    }
}
