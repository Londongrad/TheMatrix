namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply
{
    public enum SyncCityResourceSupplyStatus
    {
        Applied = 0,
        Deferred = 1,
        Stale = 2,
        NotInitialized = 3,
        Concurrent = 4
    }
}
