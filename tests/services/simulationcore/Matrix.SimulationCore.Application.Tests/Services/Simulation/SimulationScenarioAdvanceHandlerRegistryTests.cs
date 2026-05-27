using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Services.Simulation;

public sealed class SimulationScenarioAdvanceHandlerRegistryTests
{
    [Fact]
    public void Resolve_ShouldReturnHandlerRegisteredForExactRuntime()
    {
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        var registry = new SimulationScenarioAdvanceHandlerRegistry([handler]);

        ISimulationScenarioAdvanceHandler resolved = registry.Resolve(ClassicCityRuntime.Key);

        Assert.Same(handler, resolved);
    }

    [Fact]
    public void Constructor_ShouldRejectDuplicateRuntimeRegistrations()
    {
        var first = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();
        var second = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new SimulationScenarioAdvanceHandlerRegistry([first, second]));

        Assert.Contains("classic-city:city", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyRuntimeKey()
    {
        var handler = new SimulationTestSupport.FakeSimulationScenarioAdvanceHandler
        {
            RuntimeKey = new SimulationRuntimeKey()
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new SimulationScenarioAdvanceHandlerRegistry([handler]));

        Assert.Contains("empty runtime key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ShouldRejectMissingRuntimeRegistration()
    {
        var registry = new SimulationScenarioAdvanceHandlerRegistry(
            Array.Empty<ISimulationScenarioAdvanceHandler>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Resolve(ClassicCityRuntime.Key));

        Assert.Contains("classic-city:city", exception.Message, StringComparison.Ordinal);
    }
}
