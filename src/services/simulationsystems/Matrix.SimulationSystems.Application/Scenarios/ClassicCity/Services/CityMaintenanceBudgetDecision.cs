namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services
{
    public sealed record CityMaintenanceBudgetDecision(
        string RequestedIntensity,
        string AppliedIntensity,
        decimal PressureIndex);
}
