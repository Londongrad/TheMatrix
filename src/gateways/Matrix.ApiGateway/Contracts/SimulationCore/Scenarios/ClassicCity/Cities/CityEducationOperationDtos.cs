namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;

public sealed record EnrollCityResidentEducationRequestDto(
    Guid ResidentId,
    Guid InstitutionId,
    string Stage);

public sealed record CompleteCityResidentEducationRequestDto(Guid ResidentId);

public sealed record WithdrawCityResidentEducationRequestDto(Guid ResidentId);

public sealed record CityEducationOperationResponseDto(
    string Status,
    Guid? EnrollmentId = null,
    string? CompletedStage = null);
