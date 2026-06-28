namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed record AdvancePatientHealthResult(
        AdvancePatientHealthStatus Status,
        int ProcessedPatients,
        int IgnoredPatients,
        int StalePatients,
        IReadOnlyList<PatientHealthProgressionResultItem> Outcomes);
}
