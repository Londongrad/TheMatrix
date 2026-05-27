using Matrix.SimulationCore.Domain.Simulation;
using NetArchTest.Rules;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Architecture;

public sealed class ScenarioIsolationTests
{
    private const string ClassicCityNamespace =
        "Matrix.SimulationCore.Domain.Scenarios.ClassicCity";

    [Fact]
    public void ScenarioNeutralDomainTypes_ShouldNotDependOnClassicCity()
    {
        TestResult result = Types
            .InAssembly(typeof(SimulationClock).Assembly)
            .That()
            .DoNotResideInNamespaceStartingWith(ClassicCityNamespace)
            .ShouldNot()
            .HaveDependencyOn(ClassicCityNamespace)
            .GetResult();

        string dependencies = result.FailingTypes is null
            ? string.Empty
            : string.Join(
                separator: Environment.NewLine,
                values: result.FailingTypes
                   .Select(type => type.FullName)
                   .Where(typeName => typeName is not null)
                   .Order());

        Assert.True(result.IsSuccessful, dependencies);
    }
}
