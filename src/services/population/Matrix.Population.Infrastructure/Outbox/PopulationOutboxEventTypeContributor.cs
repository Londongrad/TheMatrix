using Matrix.Population.Contracts.Events;

namespace Matrix.Population.Infrastructure.Outbox
{
    public sealed class PopulationOutboxEventTypeContributor : IOutboxEventTypeContributor
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get; } =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [PopulationOutboxEventTypes.PopulationResidentFactsBatchV1] =
                    typeof(PopulationResidentFactsBatchV1),
                [PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV1] =
                    typeof(PopulationResidentHealthRiskBatchV1),
                [PopulationOutboxEventTypes.PopulationResidentHealthRiskBatchV2] =
                    typeof(PopulationResidentHealthRiskBatchV2),
                [PopulationOutboxEventTypes.PopulationResidentVitalStateBatchV1] =
                    typeof(PopulationResidentVitalStateBatchV1)
            };
    }
}
