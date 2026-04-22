namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure
{
    public enum SyncCityOperationalBudgetPressureStatus
    {
        Applied = 0,
        Stale = 1,
        NotInitialized = 2,
        Concurrent = 3
    }
}
