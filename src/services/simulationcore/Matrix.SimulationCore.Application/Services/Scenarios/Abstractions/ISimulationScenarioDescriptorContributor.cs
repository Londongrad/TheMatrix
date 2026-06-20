using Matrix.SimulationCore.Application.Services.Scenarios;

namespace Matrix.SimulationCore.Application.Services.Scenarios.Abstractions
{
    public interface ISimulationScenarioDescriptorContributor
    {
        SimulationScenarioDescriptor Descriptor { get; }
    }
}
