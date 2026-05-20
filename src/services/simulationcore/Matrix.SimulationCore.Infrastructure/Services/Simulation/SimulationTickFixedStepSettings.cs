using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Matrix.SimulationCore.Infrastructure.Services.Simulation
{
    internal sealed class SimulationTickFixedStepSettings(IOptions<SimulationTickOptions> options)
        : ISimulationFixedStepSettings
    {
        public int FixedStepSeconds => options.Value.FixedStepSeconds;
        public int MaxStepsPerSimulationPerCycle => options.Value.MaxStepsPerSimulationPerCycle;
    }
}
