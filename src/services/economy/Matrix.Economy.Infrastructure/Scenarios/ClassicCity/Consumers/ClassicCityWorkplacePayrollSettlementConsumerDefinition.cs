using MassTransit;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityWorkplacePayrollSettlementConsumerDefinition
        : ConsumerDefinition<ClassicCityWorkplacePayrollSettlementConsumer>
    {
        public ClassicCityWorkplacePayrollSettlementConsumerDefinition()
        {
            EndpointName = "economy-classic-city-workplace-payroll-settlement";
            ConcurrentMessageLimit = 1;
        }
    }
}
