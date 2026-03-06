using Matrix.ApiGateway.Contracts.CityCore.Dashboard;
using Matrix.ApiGateway.Services.CityCore.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.CityCore.Dashboard
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard/citycore")]
    public sealed class CityOperationsDashboardController(ICityOperationsDashboardService dashboardService) : ControllerBase
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
