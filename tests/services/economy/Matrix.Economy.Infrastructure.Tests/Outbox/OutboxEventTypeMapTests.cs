using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Infrastructure.Outbox;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Outbox
{
    public sealed class OutboxEventTypeMapTests
    {
        [Fact]
        public void Map_ContainsAllSupportedEconomyOutboxEventTypes()
        {
            IReadOnlyDictionary<string, Type> map = OutboxEventTypeMap.Map;

            Assert.Equal(
                expected: 5,
                actual: map.Count);
            Assert.Equal(
                expected: typeof(ClassicCityOperationalBudgetPressureSnapshotV1),
                actual: map[EconomyOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1]);
            Assert.Equal(
                expected: typeof(ClassicCityCostOfLivingSnapshotV1),
                actual: map[EconomyOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1]);
            Assert.Equal(
                expected: typeof(ClassicCityServiceQualitySnapshotV1),
                actual: map[EconomyOutboxEventTypes.ClassicCityServiceQualitySnapshotV1]);
            Assert.Equal(
                expected: typeof(ClassicCityEmployerFinancialStressBatchV1),
                actual: map[EconomyOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1]);
            Assert.Equal(
                expected: typeof(ClassicCityHouseholdFinancialStressBatchV1),
                actual: map[EconomyOutboxEventTypes.ClassicCityHouseholdFinancialStressBatchV1]);
        }
    }
}
