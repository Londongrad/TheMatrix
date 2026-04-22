using Matrix.ApiGateway.Contracts.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Budget.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Economy
{
    [Authorize]
    [ApiController]
    [Route("api/economy")]
    public class EconomyController(IEconomyApiClient economyClient) : ControllerBase
    {
        private readonly IEconomyApiClient _economyClient = economyClient;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            EconomySummaryView? summary = await _economyClient.GetSummaryAsync(cancellationToken);

            if (summary is null)
                return StatusCode(StatusCodes.Status502BadGateway);

            return Ok(summary);
        }

        [HttpGet("cities/{cityId:guid}/summary")]
        public async Task<IActionResult> GetCitySummary(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            EconomySummaryView? summary = await _economyClient.GetCitySummaryAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (summary is null)
                return StatusCode(StatusCodes.Status502BadGateway);

            return Ok(summary);
        }

        [HttpGet("budget/cities/{cityId:guid}/summary")]
        public async Task<IActionResult> GetBudgetCitySummary(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            EconomySummaryView? summary = await _economyClient.GetCitySummaryAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (summary is null)
                return StatusCode(StatusCodes.Status502BadGateway);

            return Ok(summary);
        }

        [HttpGet("budget/cities/{cityId:guid}/operational-pressure")]
        public async Task<IActionResult> GetBudgetOperationalPressure(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityOperationalBudgetPressureView? pressure = await _economyClient.GetCityOperationalBudgetPressureAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (pressure is null)
                return StatusCode(StatusCodes.Status502BadGateway);

            return Ok(pressure);
        }

        [HttpGet("budget/cities/{cityId:guid}/ledger-feed")]
        public async Task<ActionResult<CursorPagedResult<BudgetLedgerEntryView>>> GetBudgetLedgerFeed(
            [FromRoute] Guid cityId,
            [FromQuery] string? cursor = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            CursorPagedResult<BudgetLedgerEntryView> result = await _economyClient.GetCityBudgetLedgerFeedAsync(
                cityId: cityId,
                cursor: cursor,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("business/cities/{cityId:guid}")]
        public async Task<ActionResult<IReadOnlyList<CityBusinessView>>> GetCityBusinesses(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBusinessView> businesses = await _economyClient.GetCityBusinessesAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(businesses);
        }

        [HttpGet("business/{businessId:guid}/ledger-feed")]
        public async Task<ActionResult<CursorPagedResult<CityBusinessLedgerEntryView>>> GetBusinessLedgerFeed(
            [FromRoute] Guid businessId,
            [FromQuery] string? cursor = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            CursorPagedResult<CityBusinessLedgerEntryView> result = await _economyClient.GetCityBusinessLedgerFeedAsync(
                businessId: businessId,
                cursor: cursor,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("householdaccounts/cities/{cityId:guid}")]
        public async Task<ActionResult<IReadOnlyList<CityHouseholdAccountView>>> GetCityHouseholdAccounts(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdAccountView> households = await _economyClient.GetCityHouseholdAccountsAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(households);
        }

        [HttpGet("householdaccounts/{householdAccountId:guid}/ledger-feed")]
        public async Task<ActionResult<CursorPagedResult<CityHouseholdAccountLedgerEntryView>>> GetHouseholdLedgerFeed(
            [FromRoute] Guid householdAccountId,
            [FromQuery] string? cursor = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            CursorPagedResult<CityHouseholdAccountLedgerEntryView> result =
                await _economyClient.GetCityHouseholdAccountLedgerFeedAsync(
                    householdAccountId: householdAccountId,
                    cursor: cursor,
                    pageSize: pageSize,
                    cancellationToken: cancellationToken);

            return Ok(result);
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            bool isHealthy = await _economyClient.HealthAsync(cancellationToken);
            return Ok(
                new
                {
                    status = isHealthy
                        ? "ok"
                        : "degraded"
                });
        }
    }
}
