using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Dashboard
{
    [Authorize]
    [ApiController]
    [Route("api/scenarios/classic-city/dashboard")]
    public sealed class CityOperationsDashboardController(ICityOperationsDashboardService dashboardService)
        : ControllerBase
    {
        private readonly ICityOperationsDashboardService _dashboardService = dashboardService;

        [HttpGet]
        public async Task<ActionResult<CityOperationsDashboardView>> Get(CancellationToken cancellationToken)
        {
            CityOperationsDashboardView watchboard = await _dashboardService.GetAsync(cancellationToken);
            return Ok(watchboard);
        }
    }
}
