using MassTransit;
using Matrix.Education.Contracts.Events;

namespace Matrix.Population.Infrastructure.Consumers.Education
{
    public sealed class EducationStudentParticipationConsumerDefinition
        : ConsumerDefinition<EducationStudentParticipationConsumer>
    {
        public const string EndpointNameValue = "population-education-participation-v1";
        public const int ConcurrentMessageLimitValue = 8;

        private static readonly TimeSpan[] RetryIntervals =
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        ];

        public EducationStudentParticipationConsumerDefinition()
        {
            EndpointName = EndpointNameValue;
            ConcurrentMessageLimit = ConcurrentMessageLimitValue;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<EducationStudentParticipationConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
            IPartitioner partitioner = endpointConfigurator.CreatePartitioner(
                ConcurrentMessageLimitValue);
            consumerConfigurator.Message<EducationStudentParticipationBatchV1>(message =>
                message.UsePartitioner(
                    partitioner,
                    partitionContext => partitionContext.Message.SimulationHostId));
        }
    }
}
