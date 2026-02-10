namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment
{
    public enum SyncCityEnvironmentStatus
    {
        Applied = 0,
        Duplicate = 1,
        Stale = 2,
        CityDeleted = 3,
        CityArchived = 4
    }
}
