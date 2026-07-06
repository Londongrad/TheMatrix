namespace Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords
{
    public sealed record InitializePatientMedicalRecordItem(
        Guid PatientId,
        int HealthScore,
        long LifecycleRevision = 0);
}
