namespace Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities
{
    public sealed record SynchronizeCareFacilitiesResult(
        SynchronizeCareFacilitiesStatus Status,
        int AddedFacilities,
        int UpdatedFacilities,
        int IgnoredFacilities)
    {
        public int ProcessedFacilities =>
            AddedFacilities + UpdatedFacilities + IgnoredFacilities;
    }
}
