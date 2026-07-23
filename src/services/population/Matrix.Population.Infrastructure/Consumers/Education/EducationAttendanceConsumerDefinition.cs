using MassTransit;
using Matrix.Education.Contracts.Events;

namespace Matrix.Population.Infrastructure.Consumers.Education;

public sealed class EducationAttendanceConsumerDefinition : ConsumerDefinition<EducationAttendanceConsumer>
{
    public EducationAttendanceConsumerDefinition()
    {
        EndpointName = "population-education-attendance-v1";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<EducationAttendanceConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)));
        var partitioner = endpointConfigurator.CreatePartitioner(8);
        consumerConfigurator.Message<EducationAttendanceEvaluatedBatchV1>(message =>
            message.UsePartitioner(partitioner, consume => consume.Message.SimulationHostId));
    }
}
