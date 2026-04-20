namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation
{
    public enum CityTickPhase
    {
        AdvanceTime = 10,
        SystemsDegradation = 20,
        IncidentGeneration = 30,
        DispatchExecution = 40,
        ResourceSettlement = 50,
        BudgetSettlement = 60,
        PopulationReaction = 70,
        Projection = 80,
        TickCompleted = 90
    }
}
