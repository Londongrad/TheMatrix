using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.Population.Infrastructure.Outbox;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Outbox;

public sealed class OutboxEventTypeMapTests
{
    [Fact]
    public void Map_ContainsAllSupportedPopulationOutboxEventTypes()
    {
        IReadOnlyDictionary<string, Type> map = OutboxEventTypeMap.Map;

        Assert.Equal(5, map.Count);
        Assert.Equal(typeof(CityEconomyDailySettlementV1), map[PopulationOutboxEventTypes.CityEconomyDailySettlementV1]);
        Assert.Equal(typeof(ClassicCityHouseholdAccountSyncBatchV1), map[PopulationOutboxEventTypes.ClassicCityHouseholdAccountSyncBatchV1]);
        Assert.Equal(typeof(ClassicCityWorkplaceBusinessSyncBatchV1), map[PopulationOutboxEventTypes.ClassicCityWorkplaceBusinessSyncBatchV1]);
        Assert.Equal(typeof(ClassicCityWorkplacePayrollSettlementBatchV1), map[PopulationOutboxEventTypes.ClassicCityWorkplacePayrollSettlementBatchV1]);
        Assert.Equal(typeof(ClassicCityHouseholdCashflowSettlementBatchV1), map[PopulationOutboxEventTypes.ClassicCityHouseholdCashflowSettlementBatchV1]);
    }
}
