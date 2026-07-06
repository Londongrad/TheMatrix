using Matrix.Population.Contracts.Events;
using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Application.Integration
{
    public static class PopulationResidentVitalStateBatchFactory
    {
        public const int DefaultBatchSize = 1000;

        public static PopulationResidentVitalStateBatchV1[] Build(
            Guid simulationHostId,
            long sourceRevision,
            IReadOnlyCollection<Person> residents,
            string correlationId,
            DateTimeOffset observedAtUtc,
            int batchSize = DefaultBatchSize)
        {
            if (simulationHostId == Guid.Empty)
                throw new ArgumentException(
                    message: "A simulation host identifier is required.",
                    paramName: nameof(simulationHostId));

            if (sourceRevision < 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(sourceRevision),
                    message: "Vital state source revisions cannot be negative.");

            ArgumentNullException.ThrowIfNull(residents);
            ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

            if (observedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Vital state observation timestamps must be expressed in UTC.",
                    paramName: nameof(observedAtUtc));

            if (batchSize <= 0 || batchSize > DefaultBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(batchSize),
                    message: $"Vital state batch sizes must be between 1 and {DefaultBatchSize}.");

            PopulationResidentVitalStateV1[] states = residents
               .OrderBy(resident => resident.Id.Value)
               .Select(resident => new PopulationResidentVitalStateV1(
                    ResidentId: resident.Id.Value,
                    HealthScore: resident.Health.Value,
                    LifecycleRevision: resident.LifecycleRevision))
               .ToArray();

            if (states.Length == 0)
                return [];

            PopulationResidentVitalStateBatchV1[] batches = states
               .Chunk(batchSize)
               .Select((chunk, index) => new PopulationResidentVitalStateBatchV1(
                    SimulationHostId: simulationHostId,
                    SourceRevision: sourceRevision,
                    ObservedAtUtc: observedAtUtc,
                    CorrelationId: correlationId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Residents: chunk))
               .ToArray();

            for (int index = 0; index < batches.Length; index++)
                batches[index] = batches[index] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }
    }
}
