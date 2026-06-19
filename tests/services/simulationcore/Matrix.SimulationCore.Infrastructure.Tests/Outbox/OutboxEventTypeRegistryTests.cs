using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox
{
    public sealed class OutboxEventTypeRegistryTests
    {
        [Fact]
        public void Resolve_WithRuntimeAndScenarioContributors_ContainsAllSupportedEventTypes()
        {
            var registry = new OutboxEventTypeRegistry(
            [
                new SimulationCoreOutboxEventTypeContributor(),
                new ClassicCityOutboxEventTypeContributor()
            ]);

            Assert.Equal(
                expected: 12,
                actual: registry.Count);
            Assert.Equal(
                expected: typeof(SimulationCreatedV1),
                actual: registry.Resolve(SimulationCoreEventTypes.SimulationCreatedV1));
            Assert.Equal(
                expected: typeof(SimulationArchivedV1),
                actual: registry.Resolve(SimulationCoreEventTypes.SimulationArchivedV1));
            Assert.Equal(
                expected: typeof(SimulationDeletedV1),
                actual: registry.Resolve(SimulationCoreEventTypes.SimulationDeletedV1));
            Assert.Equal(
                expected: typeof(SimulationTickPhaseReachedV1),
                actual: registry.Resolve(SimulationCoreEventTypes.SimulationTickPhaseReachedV1));
            Assert.Equal(
                expected: typeof(ClassicCityCreatedV1),
                actual: registry.Resolve(ClassicCityEventTypes.ClassicCityCreatedV1));
            Assert.Equal(
                expected: typeof(CityEnvironmentChangedV1),
                actual: registry.Resolve(ClassicCityEventTypes.CityEnvironmentChangedV1));
            Assert.Equal(
                expected: typeof(CityWeatherCreatedV1),
                actual: registry.Resolve(ClassicCityEventTypes.CityWeatherCreatedV1));
            Assert.Equal(
                expected: typeof(CityWeatherChangedV1),
                actual: registry.Resolve(ClassicCityEventTypes.CityWeatherChangedV1));
            Assert.Equal(
                expected: typeof(WeatherOverrideStartedV1),
                actual: registry.Resolve(ClassicCityEventTypes.WeatherOverrideStartedV1));
            Assert.Equal(
                expected: typeof(WeatherOverrideCancelledV1),
                actual: registry.Resolve(ClassicCityEventTypes.WeatherOverrideCancelledV1));
            Assert.Equal(
                expected: typeof(WeatherOverrideExpiredV1),
                actual: registry.Resolve(ClassicCityEventTypes.WeatherOverrideExpiredV1));
            Assert.Equal(
                expected: typeof(ClimateProfileChangedV1),
                actual: registry.Resolve(ClassicCityEventTypes.ClimateProfileChangedV1));
        }

        [Fact]
        public void Constructor_WhenContributorsDuplicateEventType_ThrowsInvalidOperationException()
        {
            var duplicate = new TestOutboxEventTypeContributor(
                SimulationCoreEventTypes.SimulationCreatedV1,
                typeof(SimulationArchivedV1));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                new OutboxEventTypeRegistry(
                [
                    new SimulationCoreOutboxEventTypeContributor(),
                    duplicate
                ]));

            Assert.Contains(
                expectedSubstring: SimulationCoreEventTypes.SimulationCreatedV1,
                actualString: exception.Message);
        }

        private sealed class TestOutboxEventTypeContributor(
            string eventType,
            Type clrType) : IOutboxEventTypeContributor
        {
            public IReadOnlyDictionary<string, Type> EventTypes { get; } =
                new Dictionary<string, Type>(StringComparer.Ordinal)
                {
                    [eventType] = clrType
                };
        }
    }
}
