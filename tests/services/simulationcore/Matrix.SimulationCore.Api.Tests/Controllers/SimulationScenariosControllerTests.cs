using Matrix.SimulationCore.Api.Controllers;
using Matrix.SimulationCore.Application.UseCases.Scenarios.ListSimulationScenarios;
using Matrix.SimulationCore.Contracts.Scenarios.Catalog.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationCore.Api.Tests.TestSupport.SimulationCoreApiTestSupport;

namespace Matrix.SimulationCore.Api.Tests.Controllers
{
    public sealed class SimulationScenariosControllerTests
    {
        [Fact]
        public async Task List_ReturnsMappedScenarioCatalog()
        {
            var sender = new FakeSender();
            sender.Handle<ListSimulationScenariosQuery, IReadOnlyList<SimulationScenarioDto>>(
                query =>
                {
                    Assert.NotNull(query);
                    return
                    [
                        new SimulationScenarioDto(
                            ScenarioKey: "classic-city",
                            HostTypeKey: "city",
                            DisplayName: "Classic City",
                            CurrentModelVersion: "classic-city-v1",
                            SupportsProvisioning: true,
                            Capabilities: ["population", "economy"])
                    ];
                });
            var controller = new SimulationScenariosController(sender);

            IResult result = await controller.List(CancellationToken.None);
            SimulationScenarioView[] views = AssertResult<SimulationScenarioView[]>(
                result: result,
                expectedStatusCode: StatusCodes.Status200OK);

            SimulationScenarioView view = Assert.Single(views);
            Assert.Equal("classic-city", view.ScenarioKey);
            Assert.Equal("city", view.HostTypeKey);
            Assert.Equal("Classic City", view.DisplayName);
            Assert.Equal("classic-city-v1", view.CurrentModelVersion);
            Assert.True(view.SupportsProvisioning);
            Assert.Equal(["population", "economy"], view.Capabilities);
            Assert.IsType<ListSimulationScenariosQuery>(Assert.Single(sender.Requests));
        }
    }
}
