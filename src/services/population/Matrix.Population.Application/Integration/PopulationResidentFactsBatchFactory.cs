using Matrix.Population.Contracts.Events;
using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Application.Integration
{
    public static class PopulationResidentFactsBatchFactory
    {
        public const int DefaultBatchSize = 1000;

        public static PopulationResidentFactsBatchV1[] Build(
            Guid simulationHostId,
            long sourceRevision,
            IReadOnlyCollection<Person> residents,
            string correlationId,
            DateTimeOffset synchronizedAtUtc,
            int batchSize = DefaultBatchSize)
        {
            if (simulationHostId == Guid.Empty)
                throw new ArgumentException(
                    message: "A simulation host identifier is required.",
                    paramName: nameof(simulationHostId));

            if (sourceRevision < 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(sourceRevision),
                    message: "Resident fact source revisions cannot be negative.");

            ArgumentNullException.ThrowIfNull(residents);
            ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

            if (synchronizedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Resident fact synchronization timestamps must be expressed in UTC.",
                    paramName: nameof(synchronizedAtUtc));

            if (batchSize <= 0 || batchSize > DefaultBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(batchSize),
                    message: $"Resident fact batch sizes must be between 1 and {DefaultBatchSize}.");

            PopulationResidentFactsV1[] facts = residents
               .OrderBy(resident => resident.Id.Value)
               .Select(resident => new PopulationResidentFactsV1(
                    ResidentId: resident.Id.Value,
                    BirthDate: resident.BirthDate,
                    Sex: resident.Sex.ToString(),
                    IsAlive: resident.IsAlive,
                    IsActive: true))
               .ToArray();

            if (facts.Length == 0)
                return [];

            PopulationResidentFactsBatchV1[] batches = facts
               .Chunk(batchSize)
               .Select((chunk, index) => new PopulationResidentFactsBatchV1(
                    SimulationHostId: simulationHostId,
                    SourceRevision: sourceRevision,
                    SynchronizedAtUtc: synchronizedAtUtc,
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
