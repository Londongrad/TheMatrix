namespace Matrix.Education.Application.Students.GetStudentEducationStatus;

public sealed record ActiveStudentEnrollmentView(
    Guid EnrollmentId,
    Guid InstitutionId,
    string InstitutionName,
    string InstitutionKind,
    Guid? LocationAnchorId,
    string Stage,
    DateOnly EnrolledOn);

public sealed record StudentEducationStatusView(
    Guid ResidentId,
    bool IsAlive,
    bool IsActive,
    string? CompletedStage,
    DateOnly? CompletedStageOn,
    ActiveStudentEnrollmentView? ActiveEnrollment);
