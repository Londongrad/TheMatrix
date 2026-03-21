using MassTransit;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityHouseholdCashflowSettlementConsumerDefinition
        : ConsumerDefinition<ClassicCityHouseholdCashflowSettlementConsumer>
    {
        public ClassicCityHouseholdCashflowSettlementConsumerDefinition()
        {
            EndpointName = "economy-classic-city-household-cashflow-settlement";
            ConcurrentMessageLimit = 1;
        }
    }
}
