using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Population
{
    [Authorize]
    [ApiController]
    [Route("api/scenarios/classic-city/population")]
    public sealed class ClassicCityPopulationController(
        IClassicCityPopulationApiClient populationClient) : ControllerBase
    {
        private readonly IClassicCityPopulationApiClient _populationClient = populationClient;

        [HttpPost("init")]
        public async Task<ActionResult<CityPopulationBootstrapSummaryDto>> InitializePopulation(
            [FromBody] InitializeCityPopulationRequest request,
            CancellationToken cancellationToken = default)
        {
            CityPopulationBootstrapSummaryDto result =
                await _populationClient.InitializeCityPopulationAsync(
                    request: request,
                    cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
