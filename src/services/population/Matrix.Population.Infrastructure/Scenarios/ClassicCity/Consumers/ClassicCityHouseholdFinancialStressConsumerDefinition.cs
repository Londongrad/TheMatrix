using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityHouseholdFinancialStressConsumerDefinition
        : ConsumerDefinition<ClassicCityHouseholdFinancialStressConsumer>
    {
        public const string EndpointNameValue = "population-classic-city-household-financial-stress";

        public ClassicCityHouseholdFinancialStressConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
