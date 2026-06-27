using Matrix.Healthcare.Domain.Patients;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Patients
{
    public sealed class PatientIllnessStateTests
    {
        [Fact]
        public void Diagnose_WhenPatientIsHealthy_StartsIllnessEpisode()
        {
            DateOnly diagnosedOn = new(2048, 5, 6);

            PatientIllnessState state = PatientIllnessState.Healthy()
               .Diagnose(IllnessKind.Infection, IllnessSeverity.Mild, diagnosedOn);

            Assert.True(state.HasActiveIllness);
            Assert.Equal(IllnessKind.Infection, state.CurrentKind);
            Assert.Equal(IllnessSeverity.Mild, state.CurrentSeverity);
            Assert.Equal(diagnosedOn, state.DiagnosedOn);
        }

        [Fact]
        public void Diagnose_WhenSameIllnessContinues_PreservesOriginalDiagnosisDate()
        {
            DateOnly diagnosedOn = new(2048, 5, 6);
            PatientIllnessState initial = PatientIllnessState.Active(
                IllnessKind.Exposure,
                IllnessSeverity.Mild,
                diagnosedOn);

            PatientIllnessState result = initial.Diagnose(
                IllnessKind.Exposure,
                IllnessSeverity.Moderate,
                diagnosedOn.AddDays(2));

            Assert.Equal(diagnosedOn, result.DiagnosedOn);
            Assert.Equal(IllnessSeverity.Moderate, result.CurrentSeverity);
        }

        [Fact]
        public void ProgressTo_WhenSeverityWouldRegress_KeepsHigherSeverity()
        {
            PatientIllnessState initial = PatientIllnessState.Active(
                IllnessKind.Exhaustion,
                IllnessSeverity.Severe,
                new DateOnly(2048, 5, 6));

            PatientIllnessState result = initial.ProgressTo(IllnessSeverity.Mild);

            Assert.Equal(IllnessSeverity.Severe, result.CurrentSeverity);
        }

        [Fact]
        public void Recover_ClearsEpisodeAndRecordsRecoveryDate()
        {
            DateOnly recoveredOn = new(2048, 5, 9);
            PatientIllnessState initial = PatientIllnessState.Active(
                IllnessKind.Stress,
                IllnessSeverity.Moderate,
                new DateOnly(2048, 5, 6));

            PatientIllnessState result = initial.Recover(recoveredOn);

            Assert.False(result.HasActiveIllness);
            Assert.Null(result.CurrentKind);
            Assert.Null(result.DiagnosedOn);
            Assert.Equal(recoveredOn, result.LastRecoveredOn);
        }

        [Fact]
        public void Active_WhenDiagnosisPredatesRecovery_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PatientIllnessState.Active(
                    IllnessKind.Infection,
                    IllnessSeverity.Mild,
                    diagnosedOn: new DateOnly(2048, 5, 5),
                    lastRecoveredOn: new DateOnly(2048, 5, 6)));
        }
    }
}
