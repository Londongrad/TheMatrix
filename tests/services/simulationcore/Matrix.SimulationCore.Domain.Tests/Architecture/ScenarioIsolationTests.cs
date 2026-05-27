using Matrix.SimulationCore.Domain.Simulation;
using NetArchTest.Rules;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Architecture;

public sealed class ScenarioIsolationTests
{
    private const string ClassicCityNamespace =
        "Matrix.SimulationCore.Domain.Scenarios.ClassicCity";

    private static readonly HashSet<string> KnownScenarioDependencies =
    [
        "Matrix.SimulationCore.Domain.Events.Simulation.SimulationClockCreatedDomainEvent",
        "Matrix.SimulationCore.Domain.Events.Simulation.SimulationPausedDomainEvent",
        "Matrix.SimulationCore.Domain.Events.Simulation.SimulationResumedDomainEvent",
        "Matrix.SimulationCore.Domain.Events.Simulation.SimulationSpeedChangedDomainEvent",
        "Matrix.SimulationCore.Domain.Events.Simulation.SimulationTimeAdvancedDomainEvent",
        "Matrix.SimulationCore.Domain.Events.Simulation.SimulationTimeJumpedDomainEvent",
        "Matrix.SimulationCore.Domain.Simulation.SimulationClock"
    ];

    [Fact]
    public void ScenarioNeutralDomainTypes_ShouldNotGainClassicCityDependencies()
    {
        TestResult result = Types
            .InAssembly(typeof(SimulationClock).Assembly)
            .That()
            .DoNotResideInNamespaceStartingWith(ClassicCityNamespace)
            .ShouldNot()
            .HaveDependencyOn(ClassicCityNamespace)
            .GetResult();

        string[] unexpectedDependencies = result.FailingTypes
            .Select(type => type.FullName)
            .Where(typeName =>
                typeName is not null &&
                !KnownScenarioDependencies.Contains(typeName))
            .Order()
            .ToArray()!;

        Assert.Empty(unexpectedDependencies);
    }
}
