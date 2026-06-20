using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.Population.Infrastructure.Outbox;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxEventTypeContributorTests
    {
        [Fact]
        public void EventTypes_ContainsAllSupportedClassicCityOutboxEvents()
        {
            var registry = new OutboxEventTypeRegistry([new ClassicCityOutboxEventTypeContributor()]);

            Assert.Equal(
                expected: 5,
                actual: registry.Count);
            Assert.Equal(
                expected: typeof(CityEconomyDailySettlementV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.CityEconomyDailySettlementV1));
            Assert.Equal(
                expected: typeof(ClassicCityHouseholdAccountSyncBatchV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1));
            Assert.Equal(
                expected: typeof(ClassicCityWorkplaceBusinessSyncBatchV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityWorkplaceBusinessSyncBatchV1));
            Assert.Equal(
                expected: typeof(ClassicCityWorkplacePayrollSettlementBatchV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityWorkplacePayrollSettlementBatchV1));
            Assert.Equal(
                expected: typeof(ClassicCityHouseholdCashflowSettlementBatchV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityHouseholdCashflowSettlementBatchV1));
        }
    }
}
