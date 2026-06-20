using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.SimulationSystems.Infrastructure.Outbox;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxEventTypeContributor : IOutboxEventTypeContributor
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get; } =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [ClassicCityOutboxEventTypes.ClassicCityOperationalExpenseIncurredV1] =
                    typeof(ClassicCityOperationalExpenseIncurredV1),
                [ClassicCityOutboxEventTypes.ClassicCityLivingConditionsSnapshotV1] =
                    typeof(ClassicCityLivingConditionsSnapshotV1),
                [ClassicCityOutboxEventTypes.ClassicCitySystemsResourceDemandSnapshotV1] =
                    typeof(ClassicCitySystemsResourceDemandSnapshotV1)
            };
    }
}
