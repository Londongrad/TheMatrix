using Matrix.SimulationCore.Application.UseCases.Scenarios.ListSimulationScenarios;
using Matrix.SimulationCore.Contracts.Scenarios.Catalog;
using Matrix.SimulationCore.Contracts.Scenarios.Catalog.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationCore.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route(SimulationScenarioApiRoutes.CatalogRoute)]
    public sealed class SimulationScenariosController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IResult> List(CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationScenarioDto> scenarios = await mediator.Send(
                request: new ListSimulationScenariosQuery(),
                cancellationToken: cancellationToken);

            return Results.Ok(scenarios.Select(Map).ToArray());
        }

        private static SimulationScenarioView Map(SimulationScenarioDto scenario)
        {
            return new SimulationScenarioView(
                ScenarioKey: scenario.ScenarioKey,
                HostTypeKey: scenario.HostTypeKey,
                DisplayName: scenario.DisplayName,
                CurrentModelVersion: scenario.CurrentModelVersion,
                SupportsProvisioning: scenario.SupportsProvisioning,
                Capabilities: scenario.Capabilities);
        }
    }
}
