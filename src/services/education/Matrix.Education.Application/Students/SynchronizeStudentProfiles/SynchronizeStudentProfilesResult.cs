namespace Matrix.Education.Application.Students.SynchronizeStudentProfiles
{
    public sealed record SynchronizeStudentProfilesResult(
        int AddedProfiles,
        int UpdatedProfiles,
        int IgnoredProfiles)
    {
        public int ProcessedProfiles => AddedProfiles + UpdatedProfiles + IgnoredProfiles;
    }
}
