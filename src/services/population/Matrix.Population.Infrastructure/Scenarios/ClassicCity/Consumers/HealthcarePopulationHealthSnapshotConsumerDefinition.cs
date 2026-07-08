using MassTransit;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class HealthcarePopulationHealthSnapshotConsumerDefinition
    : ConsumerDefinition<HealthcarePopulationHealthSnapshotConsumer>
{
    public const string EndpointNameValue = "population-healthcare-population-health-snapshot-v1";
    public const int ConcurrentMessageLimitValue = 1;

    public HealthcarePopulationHealthSnapshotConsumerDefinition()
    {
        EndpointName = EndpointNameValue;
        ConcurrentMessageLimit = ConcurrentMessageLimitValue;
    }
}
