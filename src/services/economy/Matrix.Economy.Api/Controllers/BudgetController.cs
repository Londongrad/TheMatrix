using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Api.Contracts.Budget;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetExpense;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetRevenue;
using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.UseCases.GetCityBudgetSummary;
using Matrix.Economy.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Economy.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/economy/[controller]")]
    public class BudgetController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        {
            BudgetSummaryDto result = await _sender.Send(new GetBudgetSummaryQuery(), cancellationToken);

            return Ok(MapSummary(result));
        }

        [HttpGet("cities/{cityId:guid}/summary")]
        public async Task<IActionResult> GetCitySummary([FromRoute] Guid cityId, CancellationToken cancellationToken)
        {
            BudgetSummaryDto result = await _sender.Send(new GetCityBudgetSummaryQuery(cityId), cancellationToken);

            return Ok(MapSummary(result));
        }

        [HttpGet("cities/{cityId:guid}/ledger")]
        public async Task<IActionResult> GetCityLedger(
            [FromRoute] Guid cityId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            PagedResult<BudgetLedgerEntryDto> result = await _sender.Send(
                new GetCityBudgetLedgerQuery(
                    CityId: cityId,
                    PageNumber: pageNumber,
                    PageSize: pageSize),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/revenue")]
        public async Task<IActionResult> RecordRevenue(
            [FromRoute] Guid cityId,
            [FromBody] RecordBudgetEntryRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParseCategory(request.Category, out CityBudgetCategory category))
            {
                return BadRequest(new { error = $"Unsupported budget category '{request.Category}'." });
            }

            BudgetLedgerEntryDto result = await _sender.Send(
                new RecordCityBudgetRevenueCommand(
                    CityId: cityId,
                    Category: category,
                    Amount: request.Amount,
                    Title: request.Title,
                    Description: request.Description,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("cities/{cityId:guid}/expense")]
        public async Task<IActionResult> RecordExpense(
            [FromRoute] Guid cityId,
            [FromBody] RecordBudgetEntryRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryParseCategory(request.Category, out CityBudgetCategory category))
            {
                return BadRequest(new { error = $"Unsupported budget category '{request.Category}'." });
            }

            BudgetLedgerEntryDto result = await _sender.Send(
                new RecordCityBudgetExpenseCommand(
                    CityId: cityId,
                    Category: category,
                    Amount: request.Amount,
                    Title: request.Title,
                    Description: request.Description,
                    UnitKind: request.UnitKind,
                    UnitCode: request.UnitCode,
                    UnitDisplayName: request.UnitDisplayName,
                    UnitSymbol: request.UnitSymbol),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });

        private static bool TryParseCategory(string rawCategory, out CityBudgetCategory category)
        {
            return Enum.TryParse(rawCategory, ignoreCase: true, out category);
        }

        private static object MapSummary(BudgetSummaryDto result)
        {
            return new
            {
                unitKind = result.UnitKind,
                unitCode = result.UnitCode,
                unitDisplayName = result.UnitDisplayName,
                unitSymbol = result.UnitSymbol,
                balance = result.Balance.Amount,
                totalTaxIncome = result.TotalTaxIncome.Amount,
                totalIncomeTaxIncome = result.TotalIncomeTaxIncome.Amount,
                totalSalesTaxIncome = result.TotalSalesTaxIncome.Amount,
                totalDirectRevenue = result.TotalDirectRevenue.Amount,
                totalCityExpenses = result.TotalCityExpenses.Amount,
                totalRetailTurnover = result.TotalRetailTurnover.Amount,
                totalGrossPayroll = result.TotalGrossPayroll.Amount,
                totalNetPayroll = result.TotalNetPayroll.Amount
            };
        }
    }
}
