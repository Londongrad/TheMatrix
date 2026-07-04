namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed record AdvancePatientHealthResult(
        AdvancePatientHealthStatus Status,
        int ProcessedPatients,
        int IgnoredPatients,
        int StalePatients,
        IReadOnlyList<PatientHealthProgressionResultItem> Outcomes,
        bool IsBatchSetComplete = false,
        bool CompletedBatchSetNow = false,
        int CareAssignmentsCreated = 0,
        int CareAssignmentsDelivered = 0,
        int CareAssignmentsCancelled = 0);
}
