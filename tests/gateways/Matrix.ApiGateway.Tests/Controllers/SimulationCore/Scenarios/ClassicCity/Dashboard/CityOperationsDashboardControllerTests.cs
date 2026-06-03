using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    public sealed class CityOperationsDashboardControllerTests
    {
        [Fact]
        public void Route_IsScopedToClassicCityScenario()
        {
            RouteAttribute route = Assert.Single(
                typeof(CityOperationsDashboardController).GetCustomAttributes(
                    attributeType: typeof(RouteAttribute),
                    inherit: true)
                   .Cast<RouteAttribute>());

            Assert.Equal(
                expected: "api/scenarios/classic-city/dashboard",
                actual: route.Template);
        }

        [Fact]
        public async Task Get_WhenCalled_ReturnsOkDashboard()
        {
            CityOperationsDashboardView dashboard = CreateCityOperationsDashboardView();
            var dashboardService = new RecordingCityOperationsDashboardService
            {
                View = dashboard
            };
            CityOperationsDashboardController controller = CreateCityOperationsDashboardController(dashboardService);

            ActionResult<CityOperationsDashboardView> actionResult = await controller.Get(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Same(
                expected: dashboard,
                actual: Assert.IsType<CityOperationsDashboardView>(ok.Value));
            Assert.Equal(
                expected: 1,
                actual: dashboardService.GetCallCount);
        }
    }
}
