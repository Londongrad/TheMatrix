namespace Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords
{
    public sealed record InitializePatientMedicalRecordsResult(
        InitializePatientMedicalRecordsStatus Status,
        int AddedRecords,
        int IgnoredRecords);
}
