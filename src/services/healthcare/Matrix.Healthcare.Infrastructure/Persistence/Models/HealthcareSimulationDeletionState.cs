using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Infrastructure.Persistence.Models
{
    public sealed class HealthcareSimulationDeletionState
    {
        private HealthcareSimulationDeletionState()
        {
        }

        public HealthcareSimulationDeletionState(
            SimulationHostId simulationHostId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            SimulationHostId = simulationHostId;
            DeletedAtUtc = EnsureUtc(deletedAtUtc);
            UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        }

        public SimulationHostId SimulationHostId { get; private set; }
        public DateTimeOffset DeletedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public void Record(
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            DateTimeOffset normalizedDeletedAtUtc = EnsureUtc(deletedAtUtc);

            if (normalizedDeletedAtUtc < DeletedAtUtc)
                return;

            DeletedAtUtc = normalizedDeletedAtUtc;
            UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        }

        private static DateTimeOffset EnsureUtc(DateTimeOffset value)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw new ArgumentException(
                    message: "Healthcare deletion timestamps must be expressed in UTC.",
                    paramName: nameof(value));
        }
    }
}
