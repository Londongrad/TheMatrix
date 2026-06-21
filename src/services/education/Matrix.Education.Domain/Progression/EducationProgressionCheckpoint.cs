using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Domain.Progression
{
    /// <summary>
    ///     One checkpoint per simulation host. Progress is deliberately not tracked per student,
    ///     keeping duplicate tick detection constant-time regardless of population size.
    /// </summary>
    public sealed class EducationProgressionCheckpoint : AggregateRoot<SimulationHostId>
    {
        private EducationProgressionCheckpoint(
            SimulationHostId simulationHostId,
            long lastCompletedTickId,
            DateTimeOffset lastCompletedAtUtc,
            DateTimeOffset updatedAtUtc)
            : base(simulationHostId)
        {
            LastCompletedTickId = EnsureTickId(lastCompletedTickId);
            LastCompletedAtUtc = EnsureUtc(lastCompletedAtUtc, nameof(lastCompletedAtUtc));
            UpdatedAtUtc = EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        }

        private EducationProgressionCheckpoint()
            : base(default(SimulationHostId))
        {
        }

        public SimulationHostId SimulationHostId => Id;
        public long LastCompletedTickId { get; private set; }
        public DateTimeOffset LastCompletedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static EducationProgressionCheckpoint CreateCompleted(
            SimulationHostId simulationHostId,
            long tickId,
            DateTimeOffset completedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new EducationProgressionCheckpoint(
                simulationHostId: simulationHostId,
                lastCompletedTickId: tickId,
                lastCompletedAtUtc: completedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public ProgressionTickDisposition Classify(long tickId)
        {
            long validatedTickId = EnsureTickId(tickId);

            if (validatedTickId == LastCompletedTickId)
                return ProgressionTickDisposition.Duplicate;

            return validatedTickId < LastCompletedTickId
                ? ProgressionTickDisposition.OutOfOrder
                : ProgressionTickDisposition.Accepted;
        }

        public void MarkCompleted(
            long tickId,
            DateTimeOffset completedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            ProgressionTickDisposition disposition = Classify(tickId);

            if (disposition != ProgressionTickDisposition.Accepted)
                throw new InvalidOperationException(
                    $"Cannot complete a {disposition.ToString().ToLowerInvariant()} education progression tick.");

            DateTimeOffset validatedCompletedAtUtc = EnsureUtc(
                completedAtUtc,
                nameof(completedAtUtc));

            if (validatedCompletedAtUtc < LastCompletedAtUtc)
                throw new InvalidOperationException(
                    "Education progression simulation time cannot move backwards.");

            LastCompletedTickId = tickId;
            LastCompletedAtUtc = validatedCompletedAtUtc;
            UpdatedAtUtc = EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        }

        private static long EnsureTickId(long value)
        {
            return value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Progression tick identifiers cannot be negative.");
        }

        private static DateTimeOffset EnsureUtc(DateTimeOffset value, string parameterName)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw new ArgumentException(
                    message: "Progression timestamps must be expressed in UTC.",
                    paramName: parameterName);
        }
    }
}
