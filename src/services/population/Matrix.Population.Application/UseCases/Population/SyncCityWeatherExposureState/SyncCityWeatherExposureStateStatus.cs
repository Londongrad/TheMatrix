namespace Matrix.Population.Application.UseCases.Population.SyncCityWeatherExposureState
{
    public enum SyncCityWeatherExposureStateStatus
    {
        Applied = 0,
        Duplicate = 1,
        OutOfOrder = 2,
        CityDeleted = 3,
        CityArchived = 4
    }
}
