using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public sealed class SimulationCoreOutboxEventTypeContributor : IOutboxEventTypeContributor
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get; } =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [SimulationCoreEventTypes.SimulationCreatedV1] = typeof(SimulationCreatedV1),
                [SimulationCoreEventTypes.SimulationArchivedV1] = typeof(SimulationArchivedV1),
                [SimulationCoreEventTypes.SimulationDeletedV1] = typeof(SimulationDeletedV1),
                [SimulationCoreEventTypes.SimulationTickPhaseReachedV1] = typeof(SimulationTickPhaseReachedV1),
                [SimulationCoreEventTypes.SimulationCareFacilityProvisioningBatchV1] =
                    typeof(SimulationCareFacilityProvisioningBatchV1),
                [SimulationCoreEventTypes.SimulationEducationInstitutionProvisioningBatchV1] =
                    typeof(SimulationEducationInstitutionProvisioningBatchV1)
            };
    }
}
