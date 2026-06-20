using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Services.Scenarios;
using Matrix.SimulationCore.Application.Services.Scenarios.Abstractions;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Scenarios
{
    public sealed class SimulationScenarioDescriptorRegistryTests
    {
        [Fact]
        public void List_ReturnsDescriptorsInStableRuntimeOrder()
        {
            SimulationScenarioDescriptor second = CreateDescriptor("scenario-b", "host-b");
            SimulationScenarioDescriptor first = CreateDescriptor("scenario-a", "host-a");
            var registry = new SimulationScenarioDescriptorRegistry(
                [new StubContributor(second), new StubContributor(first)]);

            Assert.Equal(
                expected: [first, second],
                actual: registry.List());
        }

        [Fact]
        public void Constructor_WhenRuntimeIsRegisteredTwice_ThrowsInvalidOperationException()
        {
            SimulationScenarioDescriptor descriptor = CreateDescriptor("scenario-a", "host-a");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                new SimulationScenarioDescriptorRegistry(
                    [new StubContributor(descriptor), new StubContributor(descriptor)]));

            Assert.Contains(
                expectedSubstring: descriptor.RuntimeKey.ToString(),
                actualString: exception.Message);
        }

        [Fact]
        public void Resolve_WhenRuntimeIsUnknown_ThrowsKeyNotFoundException()
        {
            var registry = new SimulationScenarioDescriptorRegistry([]);
            var runtimeKey = new SimulationRuntimeKey(
                new SimulationScenarioKey("unknown-scenario"),
                new SimulationHostTypeKey("unknown-host"));

            KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() =>
                registry.Resolve(runtimeKey));

            Assert.Contains(
                expectedSubstring: runtimeKey.ToString(),
                actualString: exception.Message);
        }

        private static SimulationScenarioDescriptor CreateDescriptor(
            string scenarioKey,
            string hostTypeKey)
        {
            return new SimulationScenarioDescriptor(
                runtimeKey: new SimulationRuntimeKey(
                    new SimulationScenarioKey(scenarioKey),
                    new SimulationHostTypeKey(hostTypeKey)),
                displayName: scenarioKey,
                currentModelVersion: new SimulationModelVersion("v1"),
                supportsProvisioning: true,
                capabilities: ["simulation"]);
        }

        private sealed class StubContributor(SimulationScenarioDescriptor descriptor)
            : ISimulationScenarioDescriptorContributor
        {
            public SimulationScenarioDescriptor Descriptor { get; } = descriptor;
        }
    }
}
