using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords
{
    public sealed record InitializePatientMedicalRecordItem(
        Guid PatientId,
        int HealthScore,
        IllnessKind? CurrentIllnessKind,
        IllnessSeverity? CurrentIllnessSeverity,
        DateOnly? DiagnosedOn,
        DateOnly? LastRecoveredOn,
        long LifecycleRevision = 0);
}
