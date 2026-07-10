namespace Matrix.Education.Contracts.Institutions
{
    public sealed record EducationInstitutionProvisioningItem(
        Guid InstitutionId,
        string Name,
        string Kind,
        int Capacity,
        bool IsActive,
        Guid? LocationAnchorId = null);

    public sealed record SynchronizeEducationInstitutionsRequest(
        long SourceRevision,
        DateTimeOffset SynchronizedAtUtc,
        IReadOnlyCollection<EducationInstitutionProvisioningItem> Institutions);
}
