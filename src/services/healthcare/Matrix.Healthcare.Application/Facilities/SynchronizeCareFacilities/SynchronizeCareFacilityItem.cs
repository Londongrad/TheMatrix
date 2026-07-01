namespace Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities
{
    public sealed record SynchronizeCareFacilityItem(
        Guid FacilityId,
        string Name,
        string Kind,
        Guid? LocationAnchorId,
        int DailyPatientCapacity,
        bool IsActive);
}
