namespace Matrix.Population.Application.Integration.Education.ApplyEducationParticipation
{
    public sealed record StudentEducationParticipationInput(
        Guid ResidentId,
        long ParticipationRevision,
        long ResidentLifecycleRevision,
        bool IsEnrolled,
        string? ActiveStage,
        Guid? InstitutionId,
        Guid? InstitutionAnchorId,
        DateOnly? EnrolledOn,
        string? CompletedStage,
        DateOnly? CompletedStageOn,
        ResidentExternalEconomicProfile? Economics = null);
}
