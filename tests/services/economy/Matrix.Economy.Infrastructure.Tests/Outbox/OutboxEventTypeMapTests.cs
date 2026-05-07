using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Infrastructure.Outbox;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Outbox;

public sealed class OutboxEventTypeMapTests
{
    [Fact]
    public void Map_ContainsAllSupportedEconomyOutboxEventTypes()
    {
        IReadOnlyDictionary<string, Type> map = OutboxEventTypeMap.Map;

        Assert.Equal(5, map.Count);
        Assert.Equal(typeof(ClassicCityOperationalBudgetPressureSnapshotV1), map[EconomyOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1]);
        Assert.Equal(typeof(ClassicCityCostOfLivingSnapshotV1), map[EconomyOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1]);
        Assert.Equal(typeof(ClassicCityServiceQualitySnapshotV1), map[EconomyOutboxEventTypes.ClassicCityServiceQualitySnapshotV1]);
        Assert.Equal(typeof(ClassicCityEmployerFinancialStressBatchV1), map[EconomyOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1]);
        Assert.Equal(typeof(ClassicCityHouseholdFinancialStressBatchV1), map[EconomyOutboxEventTypes.ClassicCityHouseholdFinancialStressBatchV1]);
    }
}
