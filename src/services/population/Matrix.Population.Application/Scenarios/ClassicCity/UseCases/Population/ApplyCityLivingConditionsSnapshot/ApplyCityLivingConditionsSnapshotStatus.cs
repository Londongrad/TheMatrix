namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot
{
    public enum ApplyCityLivingConditionsSnapshotStatus
    {
        Applied = 0,
        Duplicate = 1,
        CityDeleted = 2,
        CityArchived = 3,
        Stale = 4
    }
}
