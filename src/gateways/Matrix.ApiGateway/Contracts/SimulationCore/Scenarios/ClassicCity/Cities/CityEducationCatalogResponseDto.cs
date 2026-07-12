namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;

public sealed record CityEducationInstitutionResponseDto(
    Guid InstitutionId,
    string Name,
    string Kind,
    Guid? LocationAnchorId,
    int Capacity,
    int CurrentEnrollmentCount,
    int AvailableSeatCount);

public sealed record CityEducationCatalogResponseDto(
    IReadOnlyList<CityEducationInstitutionResponseDto> Institutions);
