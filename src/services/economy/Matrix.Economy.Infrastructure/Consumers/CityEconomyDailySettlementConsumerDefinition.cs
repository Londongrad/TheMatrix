using MassTransit;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityEconomyDailySettlementConsumerDefinition
        : ConsumerDefinition<CityEconomyDailySettlementConsumer>
    {
        public CityEconomyDailySettlementConsumerDefinition()
        {
            EndpointName = "economy-city-daily-settlement";
            ConcurrentMessageLimit = 1;
        }
    }
}
