namespace Matrix.Education.Contracts.Institutions;

public sealed record EducationInstitutionResponse(
    Guid InstitutionId,
    string Name,
    string Kind,
    Guid? LocationAnchorId,
    int Capacity,
    int CurrentEnrollmentCount,
    int AvailableSeatCount);

public sealed record EducationInstitutionCatalogResponse(
    IReadOnlyList<EducationInstitutionResponse> Institutions);
