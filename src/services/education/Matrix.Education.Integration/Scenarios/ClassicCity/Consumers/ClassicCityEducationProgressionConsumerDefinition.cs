using MassTransit;
using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.Education.Integration.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityEducationProgressionConsumerDefinition
        : ConsumerDefinition<ClassicCityEducationProgressionConsumer>
    {
        public const string EndpointNameValue = "education-classic-city-progression-v1";
        public const int ConcurrentMessageLimitValue = 8;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public ClassicCityEducationProgressionConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ClassicCityEducationProgressionConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
            IPartitioner partitioner = endpointConfigurator.CreatePartitioner(
                ConcurrentMessageLimitValue);
            consumerConfigurator.Message<SimulationTickPhaseReachedV1>(message =>
                message.UsePartitioner(
                    partitioner,
                    context => context.Message.HostId));
        }
    }
}
