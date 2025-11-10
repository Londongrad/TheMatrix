using Matrix.Population.Application.UseCases.GenerateMonthlyIncomeForMonth;
using Matrix.Population.Application.UseCases.InitializePopulation;
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
        /// «апускает расчЄт мес€чного дохода дл€ всех работающих жителей
        /// и публикует событи€ дл€ Economy.
        /// </summary>
        [HttpPost("income/month/{month:int}")]
        public async Task<IActionResult> GenerateMonthlyIncome(int month, CancellationToken cancellationToken)
        {
            await _sender.Send(new GenerateMonthlyIncomeForMonthCommand(month), cancellationToken);
            return Accepted(); // 202 Ц команда прин€та
        }

        [HttpPost("init")]
        public async Task<IActionResult> InitializePopulation(
            [FromQuery] int peopleCount = 10_000,
            [FromQuery] int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            await _sender.Send(
                new InitializePopulationCommand(peopleCount, randomSeed),
                cancellationToken);

            return Accepted(new { message = "Population initialization started." });
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });
    }
}