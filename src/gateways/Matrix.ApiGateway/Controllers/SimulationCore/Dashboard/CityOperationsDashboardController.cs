using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.SimulationCore.Dashboard
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard/simulationcore")]
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
