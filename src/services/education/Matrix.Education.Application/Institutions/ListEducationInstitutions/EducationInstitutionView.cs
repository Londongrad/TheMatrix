namespace Matrix.Education.Application.Institutions.ListEducationInstitutions;

public sealed record EducationInstitutionView(
    Guid InstitutionId,
    string Name,
    string Kind,
    Guid? LocationAnchorId,
    int Capacity,
    int CurrentEnrollmentCount,
    int AvailableSeatCount);
