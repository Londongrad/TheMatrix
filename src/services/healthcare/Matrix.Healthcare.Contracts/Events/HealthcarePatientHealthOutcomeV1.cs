namespace Matrix.Healthcare.Contracts.Events
{
    public sealed record HealthcarePatientHealthOutcomeV1(
        Guid PatientId,
        int HealthScore,
        string? CurrentIllnessKind,
        string? CurrentIllnessSeverity,
        DateOnly? DiagnosedOn,
        DateOnly? LastRecoveredOn,
        int HealthDelta,
        int HappinessDelta,
        int EnergyDelta,
        int StressDelta,
        bool BecameCritical);
}
