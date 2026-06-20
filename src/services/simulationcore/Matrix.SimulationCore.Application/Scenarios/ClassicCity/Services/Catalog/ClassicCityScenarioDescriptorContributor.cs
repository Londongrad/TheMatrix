using Matrix.SimulationCore.Application.Services.Scenarios;
using Matrix.SimulationCore.Application.Services.Scenarios.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Catalog
{
    public sealed class ClassicCityScenarioDescriptorContributor
        : ISimulationScenarioDescriptorContributor
    {
        public SimulationScenarioDescriptor Descriptor { get; } =
            new(
                runtimeKey: ClassicCityRuntime.Key,
                displayName: "Classic City",
                currentModelVersion: new SimulationModelVersion(ScenarioModelSetVersion.DefaultValue),
                supportsProvisioning: true,
                capabilities: ClassicCityScenarioCapabilities.All);
    }
}
