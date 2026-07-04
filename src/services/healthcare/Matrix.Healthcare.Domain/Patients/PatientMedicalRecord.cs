using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Domain.Patients
{
    public sealed class PatientMedicalRecord : AggregateRoot<PatientId>
    {
        private PatientMedicalRecord(
            PatientId patientId,
            SimulationHostId simulationHostId,
            HealthScore health,
            PatientIllnessState illness,
            long lifecycleRevision)
            : base(patientId)
        {
            SimulationHostId = simulationHostId;
            Health = health;
            Illness = illness ?? throw new ArgumentNullException(nameof(illness));
            LastProgressionRevision = -1;
            LastLifecycleRevision = EnsureRevision(lifecycleRevision);
        }

        private PatientMedicalRecord()
            : base(default(PatientId))
        {
            Illness = null!;
        }

        public PatientId PatientId => Id;
        public SimulationHostId SimulationHostId { get; private set; }
        public HealthScore Health { get; private set; }
        public PatientIllnessState Illness { get; private set; }
        public long LastProgressionRevision { get; private set; } = -1;
        public long LastLifecycleRevision { get; private set; }

        public bool HasActiveIllness => Illness.HasActiveIllness;
        public bool IsCritical => Health.Value == HealthScore.Minimum;

        public static PatientMedicalRecord Register(
            PatientId patientId,
            SimulationHostId simulationHostId,
            HealthScore health,
            PatientIllnessState illness,
            long lifecycleRevision = 0)
        {
            return new PatientMedicalRecord(
                patientId: patientId,
                simulationHostId: simulationHostId,
                health: health,
                illness: illness,
                lifecycleRevision: lifecycleRevision);
        }

        public bool TrySynchronizeLifecycleState(
            long lifecycleRevision,
            long sourceRevision,
            HealthScore health,
            PatientIllnessState illness)
        {
            long normalizedLifecycleRevision = EnsureRevision(lifecycleRevision);
            long normalizedSourceRevision = EnsureRevision(sourceRevision);
            ArgumentNullException.ThrowIfNull(illness);

            if (normalizedLifecycleRevision <= LastLifecycleRevision)
                return false;

            Health = health;
            Illness = illness;
            LastLifecycleRevision = normalizedLifecycleRevision;
            LastProgressionRevision = Math.Max(LastProgressionRevision, normalizedSourceRevision);

            return true;
        }

        public void ApplyHealthDelta(int delta)
        {
            Health = Health.ApplyDelta(delta);
        }

        public bool TryAcceptProgressionRevision(long revision)
        {
            if (revision < 0)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (revision <= LastProgressionRevision)
                return false;

            LastProgressionRevision = revision;
            return true;
        }

        public void DiagnoseIllness(
            IllnessKind kind,
            IllnessSeverity severity,
            DateOnly currentDate)
        {
            Illness = Illness.Diagnose(kind, severity, currentDate);
        }

        public void ProgressIllness(IllnessSeverity severity)
        {
            Illness = Illness.ProgressTo(severity);
        }

        public void ImproveIllness(IllnessSeverity severity)
        {
            Illness = Illness.ImproveTo(severity);
        }

        public void RecoverFromIllness(DateOnly currentDate)
        {
            Illness = Illness.Recover(currentDate);
        }

        private static long EnsureRevision(long value)
        {
            return value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Lifecycle and source revisions cannot be negative.");
        }
    }
}
