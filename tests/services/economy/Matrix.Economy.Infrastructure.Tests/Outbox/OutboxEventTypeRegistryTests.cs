using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Matrix.Economy.Infrastructure.Outbox;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Outbox
{
    public sealed class OutboxEventTypeRegistryTests
    {
        [Fact]
        public void Registry_ContainsAllClassicCityOutboxEventTypes()
        {
            var registry = new OutboxEventTypeRegistry([new ClassicCityOutboxEventTypeContributor()]);

            Assert.Equal(
                expected: 5,
                actual: registry.Count);
            Assert.Equal(
                expected: typeof(ClassicCityOperationalBudgetPressureSnapshotV1),
                actual: registry.Resolve(
                    ClassicCityOutboxEventTypes.ClassicCityOperationalBudgetPressureSnapshotV1));
            Assert.Equal(
                expected: typeof(ClassicCityCostOfLivingSnapshotV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityCostOfLivingSnapshotV1));
            Assert.Equal(
                expected: typeof(ClassicCityServiceQualitySnapshotV1),
                actual: registry.Resolve(ClassicCityOutboxEventTypes.ClassicCityServiceQualitySnapshotV1));
            Assert.Equal(
                expected: typeof(ClassicCityEmployerFinancialStressBatchV1),
                actual: registry.Resolve(
                    ClassicCityOutboxEventTypes.ClassicCityEmployerFinancialStressBatchV1));
            Assert.Equal(
                expected: typeof(ClassicCityHouseholdFinancialStressBatchV1),
                actual: registry.Resolve(
                    ClassicCityOutboxEventTypes.ClassicCityHouseholdFinancialStressBatchV1));
        }
    }
}
