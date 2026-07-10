namespace Matrix.Education.Contracts.Institutions
{
    public sealed record SynchronizeEducationInstitutionsResponse(
        string Status,
        int AddedInstitutions,
        int UpdatedInstitutions,
        int IgnoredInstitutions);
}
