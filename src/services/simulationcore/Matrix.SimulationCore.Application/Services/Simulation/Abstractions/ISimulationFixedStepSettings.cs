namespace Matrix.SimulationCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationFixedStepSettings
    {
        int FixedStepSeconds { get; }
        int MaxStepsPerSimulationPerCycle { get; }
    }
}
