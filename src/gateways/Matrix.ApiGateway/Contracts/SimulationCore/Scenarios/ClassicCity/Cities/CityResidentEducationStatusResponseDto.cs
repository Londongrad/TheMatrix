namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;

public sealed record CityResidentActiveEnrollmentResponseDto(
    Guid EnrollmentId,
    Guid InstitutionId,
    string InstitutionName,
    string InstitutionKind,
    Guid? LocationAnchorId,
    string Stage,
    DateOnly EnrolledOn);

public sealed record CityResidentEducationStatusResponseDto(
    Guid ResidentId,
    bool ProfileAvailable,
    bool IsAlive,
    bool IsActive,
    string? CompletedStage,
    DateOnly? CompletedStageOn,
    CityResidentActiveEnrollmentResponseDto? ActiveEnrollment);
