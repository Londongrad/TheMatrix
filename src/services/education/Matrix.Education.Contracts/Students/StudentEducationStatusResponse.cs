namespace Matrix.Education.Contracts.Students;

public sealed record ActiveStudentEnrollmentResponse(
    Guid EnrollmentId,
    Guid InstitutionId,
    string InstitutionName,
    string InstitutionKind,
    Guid? LocationAnchorId,
    string Stage,
    DateOnly EnrolledOn);

public sealed record StudentEducationStatusResponse(
    Guid ResidentId,
    bool IsAlive,
    bool IsActive,
    string? CompletedStage,
    DateOnly? CompletedStageOn,
    ActiveStudentEnrollmentResponse? ActiveEnrollment);
