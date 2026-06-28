using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Patients
{
    public sealed class PatientMedicalRecordTests
    {
        private static readonly PatientId PatientId = new(Guid.NewGuid());
        private static readonly SimulationHostId SimulationHostId = new(Guid.NewGuid());

        [Fact]
        public void Register_PreservesInitialMedicalState()
        {
            PatientIllnessState illness = PatientIllnessState.Active(
                IllnessKind.Infection,
                IllnessSeverity.Moderate,
                new DateOnly(2048, 5, 6));

            PatientMedicalRecord record = PatientMedicalRecord.Register(
                PatientId,
                SimulationHostId,
                new HealthScore(63),
                illness);

            Assert.Equal(PatientId, record.PatientId);
            Assert.Equal(SimulationHostId, record.SimulationHostId);
            Assert.Equal(63, record.Health.Value);
            Assert.Same(illness, record.Illness);
            Assert.True(record.HasActiveIllness);
            Assert.Equal(-1, record.LastProgressionRevision);
        }

        [Fact]
        public void ApplyHealthDelta_WhenHealthReachesMinimum_MarksRecordCritical()
        {
            PatientMedicalRecord record = CreateHealthyRecord(health: 12);

            record.ApplyHealthDelta(delta: -20);

            Assert.Equal(HealthScore.Minimum, record.Health.Value);
            Assert.True(record.IsCritical);
        }

        [Fact]
        public void IllnessLifecycle_IsOwnedByMedicalRecord()
        {
            DateOnly diagnosedOn = new(2048, 5, 6);
            DateOnly recoveredOn = diagnosedOn.AddDays(3);
            PatientMedicalRecord record = CreateHealthyRecord();

            record.DiagnoseIllness(IllnessKind.Exposure, IllnessSeverity.Mild, diagnosedOn);
            record.ProgressIllness(IllnessSeverity.Severe);
            record.RecoverFromIllness(recoveredOn);

            Assert.False(record.HasActiveIllness);
            Assert.Equal(recoveredOn, record.Illness.LastRecoveredOn);
        }

        [Fact]
        public void TryAcceptProgressionRevision_AcceptsOnlyMonotonicRevision()
        {
            PatientMedicalRecord record = CreateHealthyRecord();

            bool first = record.TryAcceptProgressionRevision(7);
            bool duplicate = record.TryAcceptProgressionRevision(7);
            bool stale = record.TryAcceptProgressionRevision(6);
            bool next = record.TryAcceptProgressionRevision(8);

            Assert.True(first);
            Assert.False(duplicate);
            Assert.False(stale);
            Assert.True(next);
            Assert.Equal(8, record.LastProgressionRevision);
        }

        [Fact]
        public void Register_WhenIllnessIsMissing_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                PatientMedicalRecord.Register(
                    PatientId,
                    SimulationHostId,
                    HealthScore.Full,
                    illness: null!));
        }

        private static PatientMedicalRecord CreateHealthyRecord(int health = HealthScore.Maximum)
        {
            return PatientMedicalRecord.Register(
                PatientId,
                SimulationHostId,
                new HealthScore(health),
                PatientIllnessState.Healthy());
        }
    }
}
