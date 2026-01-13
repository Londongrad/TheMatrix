using Matrix.Economy.Application.UseCases.GetBudgetSummary;
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
            var result = await _sender.Send(new GetBudgetSummaryQuery(), cancellationToken);

            return Ok(new
            {
                balance = result.Balance.Amount,
                totalTaxIncome = result.TotalTaxIncome.Amount
            });
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });
    }
}
