using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetSimulationKinds
{
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

            IReadOnlyList<SimulationKindCatalogItemDto> result = await handler.Handle(
                request: new GetSimulationKindsQuery(),
                cancellationToken: CancellationToken.None);

            SimulationKindCatalogItemDto item = Assert.Single(result);
            Assert.Equal(
                expected: "ClassicCity",
                actual: item.Kind);
            Assert.Equal(
                expected: "Classic City",
                actual: item.DisplayName);
            Assert.Equal(
                expected: "Default classic city flow.",
                actual: item.Description);
            Assert.True(item.SupportsAutomaticPopulationBootstrap);
            Assert.True(item.IsDefault);
        }
    }
}
