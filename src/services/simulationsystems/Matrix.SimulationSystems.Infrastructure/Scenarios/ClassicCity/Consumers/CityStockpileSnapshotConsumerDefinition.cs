using MassTransit;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityStockpileSnapshotConsumerDefinition : ConsumerDefinition<CityStockpileSnapshotConsumer>
    {
        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public CityStockpileSnapshotConsumerDefinition()
        {
            EndpointName = "simulation-systems-city-stockpile-snapshot";
            ConcurrentMessageLimit = 1;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<CityStockpileSnapshotConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
        }
    }
}
