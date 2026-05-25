namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions
{
    public enum RecalculateCityEnvironmentalConditionsStatus
    {
        Applied = 0,
        Duplicate = 1,
        Stale = 2,
        NotInitialized = 3
    }
}
