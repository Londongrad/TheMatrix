using Matrix.Population.Application.UseCases.GenerateMonthlyIncomeForMonth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers
{
    [ApiController]
    [Route("api/population/[controller]")]
    public class SimulationController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        /// <summary>
        /// Запускает расчёт месячного дохода для всех работающих жителей
        /// и публикует события для Economy.
        /// </summary>
        [HttpPost("income/month/{month:int}")]
        public async Task<IActionResult> GenerateMonthlyIncome(int month, CancellationToken cancellationToken)
        {
            await _sender.Send(new GenerateMonthlyIncomeForMonthCommand(month), cancellationToken);
            return Accepted(); // 202 – команда принята
        }

        /// <summary>
        /// Простейший health-check для фронта/оркестратора.
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });
    }
}