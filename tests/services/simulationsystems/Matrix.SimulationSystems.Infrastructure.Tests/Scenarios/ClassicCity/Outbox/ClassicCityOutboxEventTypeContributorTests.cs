using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources;
using Matrix.SimulationSystems.Infrastructure.Outbox;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox;
using Xunit;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxEventTypeContributorTests
    {
        [Fact]
        public void EventTypes_ContainsClassicCityIntegrationEvents()
        {
            var contributor = new ClassicCityOutboxEventTypeContributor();

            Assert.Equal(
                expected: typeof(ClassicCityOperationalExpenseIncurredV1),
                actual: contributor.EventTypes[
                    ClassicCityOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1]);
            Assert.Equal(
                expected: typeof(ClassicCityLivingConditionsSnapshotV1),
                actual: contributor.EventTypes[
                    ClassicCityOutboxEventTypes.ClassicCityLivingConditionsSnapshotV1]);
            Assert.Equal(
                expected: typeof(ClassicCitySystemsResourceDemandSnapshotV1),
                actual: contributor.EventTypes[
                    ClassicCityOutboxEventTypes.ClassicCitySystemsResourceDemandSnapshotV1]);
        }
    }
}
