using Matrix.SimulationCore.Application.Services.Scenarios;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Scenarios.ListSimulationScenarios
{
    public sealed class ListSimulationScenariosQueryHandler(
        SimulationScenarioDescriptorRegistry descriptorRegistry)
        : IRequestHandler<ListSimulationScenariosQuery, IReadOnlyList<SimulationScenarioDto>>
    {
        public Task<IReadOnlyList<SimulationScenarioDto>> Handle(
            ListSimulationScenariosQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationScenarioDto> scenarios = descriptorRegistry
               .List()
               .Select(Map)
               .ToArray();

            return Task.FromResult(scenarios);
        }

        private static SimulationScenarioDto Map(SimulationScenarioDescriptor descriptor)
        {
            return new SimulationScenarioDto(
                ScenarioKey: descriptor.RuntimeKey.ScenarioKey.Value,
                HostTypeKey: descriptor.RuntimeKey.HostTypeKey.Value,
                DisplayName: descriptor.DisplayName,
                CurrentModelVersion: descriptor.CurrentModelVersion.Value,
                SupportsProvisioning: descriptor.SupportsProvisioning,
                Capabilities: descriptor.Capabilities);
        }
    }
}
