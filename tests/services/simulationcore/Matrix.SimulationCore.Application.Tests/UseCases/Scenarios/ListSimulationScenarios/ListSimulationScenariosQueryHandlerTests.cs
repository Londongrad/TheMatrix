using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Services.Scenarios;
using Matrix.SimulationCore.Application.Services.Scenarios.Abstractions;
using Matrix.SimulationCore.Application.UseCases.Scenarios.ListSimulationScenarios;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Scenarios.ListSimulationScenarios
{
    public sealed class ListSimulationScenariosQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsRegisteredDescriptorsInRegistryOrder()
        {
            var registry = new SimulationScenarioDescriptorRegistry(
            [
                Contributor(
                    scenarioKey: "second",
                    hostTypeKey: "network",
                    capability: "routing"),
                Contributor(
                    scenarioKey: "first",
                    hostTypeKey: "city",
                    capability: "population")
            ]);
            var handler = new ListSimulationScenariosQueryHandler(registry);

            IReadOnlyList<SimulationScenarioDto> result = await handler.Handle(
                request: new ListSimulationScenariosQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Collection(
                collection: result,
                scenario =>
                {
                    Assert.Equal("first", scenario.ScenarioKey);
                    Assert.Equal("city", scenario.HostTypeKey);
                    Assert.Equal("First", scenario.DisplayName);
                    Assert.Equal("v1", scenario.CurrentModelVersion);
                    Assert.True(scenario.SupportsProvisioning);
                    Assert.Equal(["population"], scenario.Capabilities);
                },
                scenario =>
                {
                    Assert.Equal("second", scenario.ScenarioKey);
                    Assert.Equal("network", scenario.HostTypeKey);
                    Assert.Equal(["routing"], scenario.Capabilities);
                });
        }

        [Fact]
        public void Query_RequiresScenarioCatalogPermission()
        {
            var query = new ListSimulationScenariosQuery();

            Assert.Equal(
                expected: PermissionKeys.SimulationCoreScenariosCatalogRead,
                actual: query.PermissionKey);
        }

        private static ISimulationScenarioDescriptorContributor Contributor(
            string scenarioKey,
            string hostTypeKey,
            string capability)
        {
            return new StubContributor(
                new SimulationScenarioDescriptor(
                    runtimeKey: new SimulationRuntimeKey(
                        scenarioKey: new SimulationScenarioKey(scenarioKey),
                        hostTypeKey: new SimulationHostTypeKey(hostTypeKey)),
                    displayName: char.ToUpperInvariant(scenarioKey[0]) + scenarioKey[1..],
                    currentModelVersion: new SimulationModelVersion("v1"),
                    supportsProvisioning: true,
                    capabilities: [capability]));
        }

        private sealed class StubContributor(SimulationScenarioDescriptor descriptor)
            : ISimulationScenarioDescriptorContributor
        {
            public SimulationScenarioDescriptor Descriptor { get; } = descriptor;
        }
    }
}
