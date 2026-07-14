using Matrix.Education.Contracts.Events;

namespace Matrix.Education.Application.Integration
{
    public static class EducationStudentParticipationBatchFactory
    {
        public const int DefaultBatchSize = 1000;

        public static EducationStudentParticipationBatchV1[] Build(
            Guid simulationHostId,
            DateOnly snapshotDate,
            DateTimeOffset occurredAtUtc,
            string correlationId,
            IReadOnlyCollection<EducationStudentParticipationChange> changes,
            int batchSize = DefaultBatchSize)
        {
            if (simulationHostId == Guid.Empty)
                throw new ArgumentException(
                    message: "A simulation host identifier is required.",
                    paramName: nameof(simulationHostId));
            if (occurredAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Participation timestamps must be expressed in UTC.",
                    paramName: nameof(occurredAtUtc));

            ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
            ArgumentNullException.ThrowIfNull(changes);

            if (batchSize <= 0 || batchSize > DefaultBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(batchSize),
                    message: $"Participation batch sizes must be between 1 and {DefaultBatchSize}.");

            EducationStudentParticipationV1[] students = changes
               .OrderBy(change => change.ResidentId)
               .Select(Map)
               .ToArray();

            if (students.Select(student => student.ResidentId).Distinct().Count() != students.Length)
                throw new ArgumentException(
                    message: "A participation batch cannot contain the same resident more than once.",
                    paramName: nameof(changes));
            if (students.Length == 0)
                return [];

            EducationStudentParticipationBatchV1[] batches = students
               .Chunk(batchSize)
               .Select((chunk, index) => new EducationStudentParticipationBatchV1(
                    SimulationHostId: simulationHostId,
                    SnapshotDate: snapshotDate,
                    OccurredAtUtc: occurredAtUtc,
                    CorrelationId: correlationId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Students: chunk))
               .ToArray();

            for (int index = 0; index < batches.Length; index++)
                batches[index] = batches[index] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }

        private static EducationStudentParticipationV1 Map(
            EducationStudentParticipationChange change)
        {
            ArgumentNullException.ThrowIfNull(change);
            if (change.ResidentId == Guid.Empty)
                throw new ArgumentException("Participation changes require a resident identifier.");
            if (change.ParticipationRevision <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(change),
                    "Published participation revisions must be positive.");
            if (change.ResidentLifecycleRevision < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(change),
                    "Resident lifecycle revisions cannot be negative.");

            bool hasCompleteEnrollment = !string.IsNullOrWhiteSpace(change.ActiveStage)
                                         && change.InstitutionId.HasValue
                                         && change.EnrolledOn.HasValue;
            bool hasAnyEnrollment = !string.IsNullOrWhiteSpace(change.ActiveStage)
                                    || change.InstitutionId.HasValue
                                    || change.InstitutionAnchorId.HasValue
                                    || change.EnrolledOn.HasValue;
            if (change.IsEnrolled != hasCompleteEnrollment
                || (!change.IsEnrolled && hasAnyEnrollment))
                throw new ArgumentException("Participation enrollment fields are inconsistent.");

            return new EducationStudentParticipationV1(
                ResidentId: change.ResidentId,
                ParticipationRevision: change.ParticipationRevision,
                ResidentLifecycleRevision: change.ResidentLifecycleRevision,
                IsEnrolled: change.IsEnrolled,
                ActiveStage: change.ActiveStage,
                InstitutionId: change.InstitutionId,
                InstitutionAnchorId: change.InstitutionAnchorId,
                EnrolledOn: change.EnrolledOn,
                CompletedStage: change.CompletedStage,
                CompletedStageOn: change.CompletedStageOn);
        }
    }
}
