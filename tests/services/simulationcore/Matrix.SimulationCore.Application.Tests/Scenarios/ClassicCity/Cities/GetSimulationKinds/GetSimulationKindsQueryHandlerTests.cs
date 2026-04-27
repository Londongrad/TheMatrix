using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetSimulationKinds;

public sealed class GetSimulationKindsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDistinctMappedCatalogItems()
    {
        var handler = new GetSimulationKindsQueryHandler(
        [
            new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
            {
                Descriptor = new SimulationKindDescriptor(
                    Kind: SimulationKind.ClassicCity,
                    DisplayName: "Classic City",
                    Description: "Default classic city flow.",
                    SupportsAutomaticPopulationBootstrap: true,
                    IsDefault: true)
            },
            new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
            {
                Descriptor = new SimulationKindDescriptor(
                    Kind: SimulationKind.ClassicCity,
                    DisplayName: "Classic City Duplicate",
                    Description: "Should be filtered by kind.",
                    SupportsAutomaticPopulationBootstrap: false,
                    IsDefault: false)
            }
        ]);

        var result = await handler.Handle(new GetSimulationKindsQuery(), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("ClassicCity", item.Kind);
        Assert.Equal("Classic City", item.DisplayName);
        Assert.Equal("Default classic city flow.", item.Description);
        Assert.True(item.SupportsAutomaticPopulationBootstrap);
        Assert.True(item.IsDefault);
    }
}
