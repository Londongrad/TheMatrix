namespace Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles
{
    public sealed record SynchronizePatientProfilesResult(
        SynchronizePatientProfilesStatus Status,
        int AddedProfiles,
        int UpdatedProfiles,
        int IgnoredProfiles)
    {
        public int ProcessedProfiles => AddedProfiles + UpdatedProfiles + IgnoredProfiles;
    }
}
