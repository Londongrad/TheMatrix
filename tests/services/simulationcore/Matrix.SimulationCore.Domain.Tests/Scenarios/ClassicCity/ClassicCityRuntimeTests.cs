using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity;

public sealed class ClassicCityRuntimeTests
{
    [Fact]
    public void Key_ShouldUseStableScenarioAndHostTypeValues()
    {
        Assert.Equal("classic-city", ClassicCityRuntime.ScenarioKey.Value);
        Assert.Equal("city", ClassicCityRuntime.HostTypeKey.Value);
        Assert.Equal("classic-city:city", ClassicCityRuntime.Key.ToString());
    }
}
