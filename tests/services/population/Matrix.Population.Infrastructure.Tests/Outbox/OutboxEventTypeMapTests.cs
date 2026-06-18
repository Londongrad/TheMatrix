using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.Population.Infrastructure.Outbox;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Outbox
{
    public sealed class OutboxEventTypeMapTests
    {
        [Fact]
        public void Map_ContainsAllSupportedPopulationOutboxEventTypes()
        {
            IReadOnlyDictionary<string, Type> map = OutboxEventTypeMap.Map;

            Assert.Equal(
                expected: 5,
                actual: map.Count);
            Assert.Equal(
                expected: typeof(CityEconomyDailySettlementV1),
                actual: map[PopulationOutboxEventTypes.CityEconomyDailySettlementV1]);
            Assert.Equal(
                expected: typeof(ClassicCityHouseholdAccountSyncBatchV1),
                actual: map[PopulationOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1]);
            Assert.Equal(
                expected: typeof(ClassicCityWorkplaceBusinessSyncBatchV1),
                actual: map[PopulationOutboxEventTypes.ClassicCityWorkplaceBusinessSyncBatchV1]);
            Assert.Equal(
                expected: typeof(ClassicCityWorkplacePayrollSettlementBatchV1),
                actual: map[PopulationOutboxEventTypes.ClassicCityWorkplacePayrollSettlementBatchV1]);
            Assert.Equal(
                expected: typeof(ClassicCityHouseholdCashflowSettlementBatchV1),
                actual: map[PopulationOutboxEventTypes.ClassicCityHouseholdCashflowSettlementBatchV1]);
        }
    }
}
