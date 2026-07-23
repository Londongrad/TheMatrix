using MassTransit;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;

namespace Matrix.Education.Integration.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityLearningAttendanceConsumerDefinition : ConsumerDefinition<ClassicCityLearningAttendanceConsumer>
{
    public ClassicCityLearningAttendanceConsumerDefinition()
    {
        EndpointName = "education-classic-city-learning-attendance-v1";
        ConcurrentMessageLimit = 8;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ClassicCityLearningAttendanceConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)));
        var partitioner = endpointConfigurator.CreatePartitioner(8);
        consumerConfigurator.Message<ClassicCityResidentActivityConditionsBatchV1>(message =>
            message.UsePartitioner(partitioner, consume => consume.Message.SimulationHostId));
    }
}
