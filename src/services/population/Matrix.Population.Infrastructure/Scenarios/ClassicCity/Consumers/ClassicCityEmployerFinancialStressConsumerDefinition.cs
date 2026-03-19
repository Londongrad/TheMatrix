using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityEmployerFinancialStressConsumerDefinition
        : ConsumerDefinition<ClassicCityEmployerFinancialStressConsumer>
    {
        public const string EndpointNameValue = "population-classic-city-employer-financial-stress";

        public ClassicCityEmployerFinancialStressConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = 1;
        }
    }
}
