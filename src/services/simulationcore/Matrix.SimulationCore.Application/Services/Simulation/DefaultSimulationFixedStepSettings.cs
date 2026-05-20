using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;

namespace Matrix.SimulationCore.Application.Services.Simulation
{
    internal sealed class DefaultSimulationFixedStepSettings : ISimulationFixedStepSettings
    {
        public int FixedStepSeconds => 60;
        public int MaxStepsPerSimulationPerCycle => 10;
    }
}
