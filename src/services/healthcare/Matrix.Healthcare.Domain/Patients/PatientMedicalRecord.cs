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
            PatientIllnessState illness)
            : base(patientId)
        {
            SimulationHostId = simulationHostId;
            Health = health;
            Illness = illness ?? throw new ArgumentNullException(nameof(illness));
            LastProgressionRevision = -1;
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

        public bool HasActiveIllness => Illness.HasActiveIllness;
        public bool IsCritical => Health.Value == HealthScore.Minimum;

        public static PatientMedicalRecord Register(
            PatientId patientId,
            SimulationHostId simulationHostId,
            HealthScore health,
            PatientIllnessState illness)
        {
            return new PatientMedicalRecord(
                patientId: patientId,
                simulationHostId: simulationHostId,
                health: health,
                illness: illness);
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

        public void RecoverFromIllness(DateOnly currentDate)
        {
            Illness = Illness.Recover(currentDate);
        }
    }
}
