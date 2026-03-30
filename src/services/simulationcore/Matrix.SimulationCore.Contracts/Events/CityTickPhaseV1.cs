namespace Matrix.SimulationCore.Contracts.Events
{
    public enum CityTickPhaseV1
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
