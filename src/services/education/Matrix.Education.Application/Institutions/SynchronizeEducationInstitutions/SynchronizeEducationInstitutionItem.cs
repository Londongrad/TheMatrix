namespace Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions
{
    public sealed record SynchronizeEducationInstitutionItem(
        Guid InstitutionId,
        string Name,
        string Kind,
        int Capacity,
        bool IsActive,
        Guid? LocationAnchorId = null);
}
