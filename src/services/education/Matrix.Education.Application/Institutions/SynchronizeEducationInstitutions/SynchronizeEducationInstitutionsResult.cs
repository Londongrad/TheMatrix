namespace Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions
{
    public sealed record SynchronizeEducationInstitutionsResult(
        SynchronizeEducationInstitutionsStatus Status,
        int AddedInstitutions,
        int UpdatedInstitutions,
        int IgnoredInstitutions);
}
