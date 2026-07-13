using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Progression;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Application.Tests.Progression
{
    public sealed class EducationProgressionBatchProcessorRegistryTests
    {
        [Fact]
        public void Resolve_ReturnsProcessorRegisteredForRuntime()
        {
            var processor = new ProcessorStub(CreateRuntimeKey("classic-city", "city"));
            var registry = new EducationProgressionBatchProcessorRegistry([processor]);

            IEducationProgressionBatchProcessor resolved = registry.Resolve(processor.RuntimeKey);

            Assert.Same(processor, resolved);
        }

        [Fact]
        public void Constructor_WhenRuntimeIsDuplicated_Throws()
        {
            SimulationRuntimeKey runtimeKey = CreateRuntimeKey("classic-city", "city");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                new EducationProgressionBatchProcessorRegistry(
                    [new ProcessorStub(runtimeKey), new ProcessorStub(runtimeKey)]));

            Assert.Contains(runtimeKey.ToString(), exception.Message);
        }

        [Fact]
        public void Resolve_WhenRuntimeIsUnsupported_Throws()
        {
            var registry = new EducationProgressionBatchProcessorRegistry(
                [new ProcessorStub(CreateRuntimeKey("classic-city", "city"))]);
            SimulationRuntimeKey metroRuntime = CreateRuntimeKey("metro-2033", "station-network");

            NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
                registry.Resolve(metroRuntime));

            Assert.Contains(metroRuntime.ToString(), exception.Message);
        }

        private static SimulationRuntimeKey CreateRuntimeKey(string scenarioKey, string hostTypeKey)
        {
            return new SimulationRuntimeKey(
                scenarioKey: new SimulationScenarioKey(scenarioKey),
                hostTypeKey: new SimulationHostTypeKey(hostTypeKey));
        }

        private sealed class ProcessorStub(SimulationRuntimeKey runtimeKey)
            : IEducationProgressionBatchProcessor
        {
            public SimulationRuntimeKey RuntimeKey { get; } = runtimeKey;

            public Task<EducationProgressionBatchResult> ProcessAsync(
                EducationProgressionBatch batch,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(EducationProgressionBatchResult.Empty);
            }
        }
    }
}
