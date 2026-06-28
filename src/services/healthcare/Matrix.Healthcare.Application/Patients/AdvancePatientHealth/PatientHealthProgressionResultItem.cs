using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed record PatientHealthProgressionResultItem(
        Guid PatientId,
        int HealthScore,
        IllnessKind? CurrentIllnessKind,
        IllnessSeverity? CurrentIllnessSeverity,
        DateOnly? DiagnosedOn,
        DateOnly? LastRecoveredOn,
        int HealthDelta,
        int HappinessDelta,
        int EnergyDelta,
        int StressDelta,
        bool BecameCritical);
}
