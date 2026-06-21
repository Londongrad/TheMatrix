using Matrix.ArchitectureTesting;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.SetCityWaterDistributionEmergencyMode;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Architecture;

public sealed class ClassicCityApplicationBoundaryTests
{
    [Fact]
    public void ScenarioNeutralApplicationTypes_DoNotDependOnClassicCity()
    {
        ScenarioDependencyRule.AssertScenarioNeutral(
            assembly: typeof(SetCityWaterDistributionEmergencyModeCommand).Assembly,
            boundedContextNamespace: "Matrix.SimulationSystems",
            scenarioName: "ClassicCity");
    }
}
