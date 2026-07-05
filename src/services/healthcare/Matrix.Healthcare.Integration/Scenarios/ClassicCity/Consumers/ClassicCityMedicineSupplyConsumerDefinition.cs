using MassTransit;

namespace Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityMedicineSupplyConsumerDefinition
    : ConsumerDefinition<ClassicCityMedicineSupplyConsumer>
{
    public const string EndpointNameValue =
        "healthcare-classic-city-medicine-supply-v1";
    public const int ConcurrentMessageLimitValue = 4;

    private static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    public ClassicCityMedicineSupplyConsumerDefinition()
    {
        EndpointName = EndpointNameValue;
        ConcurrentMessageLimit = ConcurrentMessageLimitValue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ClassicCityMedicineSupplyConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(RetryIntervals));
    }
}
