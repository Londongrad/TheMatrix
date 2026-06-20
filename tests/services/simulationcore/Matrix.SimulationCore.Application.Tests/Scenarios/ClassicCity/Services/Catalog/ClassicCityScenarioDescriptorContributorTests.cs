using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Catalog;
using Matrix.SimulationCore.Application.Services.Scenarios;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Catalog
{
    public sealed class ClassicCityScenarioDescriptorContributorTests
    {
        [Fact]
        public void Descriptor_DeclaresClassicCityRuntimeAndCapabilities()
        {
            SimulationScenarioDescriptor descriptor =
                new ClassicCityScenarioDescriptorContributor().Descriptor;

            Assert.Equal(
                expected: ClassicCityRuntime.Key,
                actual: descriptor.RuntimeKey);
            Assert.Equal(
                expected: "Classic City",
                actual: descriptor.DisplayName);
            Assert.Equal(
                expected: ScenarioModelSetVersion.DefaultValue,
                actual: descriptor.CurrentModelVersion.Value);
            Assert.True(descriptor.SupportsProvisioning);
            Assert.Equal(
                expected: ClassicCityScenarioCapabilities.All,
                actual: descriptor.Capabilities);
        }
    }
}
