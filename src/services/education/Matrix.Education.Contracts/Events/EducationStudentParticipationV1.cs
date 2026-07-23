namespace Matrix.Education.Contracts.Events
{
    public sealed record EducationStudentParticipationV1(
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
        EducationEconomicEffectsV1? EconomicEffects = null,
        EducationDailyRoutineV1? DailyRoutine = null);
}
