using Matrix.Economy.Application.UseCases.GetBudgetSummary;
using Matrix.Economy.Application.UseCases.GetCityBudgetSummary;
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

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });

        private static object MapSummary(BudgetSummaryDto result)
        {
            return new
            {
                balance = result.Balance.Amount,
                totalTaxIncome = result.TotalTaxIncome.Amount,
                totalIncomeTaxIncome = result.TotalIncomeTaxIncome.Amount,
                totalSalesTaxIncome = result.TotalSalesTaxIncome.Amount,
                totalRetailTurnover = result.TotalRetailTurnover.Amount,
                totalGrossPayroll = result.TotalGrossPayroll.Amount,
                totalNetPayroll = result.TotalNetPayroll.Amount
            };
        }
    }
}
