using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.Controllers.SimulationCore.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.SimulationCore
{
    public sealed class CityOperationsDashboardControllerTests
    {
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
