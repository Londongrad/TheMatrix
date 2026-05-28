using Matrix.Simulation.Primitives;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;

internal static class CityTickPhaseExtensions
{
    public static SimulationPhaseKey ToPhaseKey(this CityTickPhase phase)
    {
        string value = phase switch
        {
            CityTickPhase.AdvanceTime => "advance-time",
            CityTickPhase.SystemsDegradation => "systems-degradation",
            CityTickPhase.IncidentGeneration => "incident-generation",
            CityTickPhase.DispatchExecution => "dispatch-execution",
            CityTickPhase.ResourceSettlement => "resource-settlement",
            CityTickPhase.BudgetSettlement => "budget-settlement",
            CityTickPhase.PopulationReaction => "population-reaction",
            CityTickPhase.Projection => "projection",
            CityTickPhase.TickCompleted => "tick-completed",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unsupported classic city tick phase.")
        };

        return new SimulationPhaseKey(value);
    }
}
