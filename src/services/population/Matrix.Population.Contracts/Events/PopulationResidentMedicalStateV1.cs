namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentMedicalStateV1(
        Guid ResidentId,
        int HealthScore,
        string? CurrentIllnessKind,
        string? CurrentIllnessSeverity,
        DateOnly? DiagnosedOn,
        DateOnly? LastRecoveredOn);
}
