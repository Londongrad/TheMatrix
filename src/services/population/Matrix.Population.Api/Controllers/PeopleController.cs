using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Matrix.Population.Contracts;
using Matrix.Population.Contracts.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers;

[ApiController]
[Authorize]
[Route(PopulationApiRoutes.PeopleRoute)]
public sealed class PeopleController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PersonDto>>> GetCitizensPage(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var pagination = new Pagination(
            pageNumber: pageNumber,
            pageSize: pageSize);

        PagedResult<PersonDto> result = await _sender.Send(
            request: new GetCitizensPageQuery(pagination),
            cancellationToken: cancellationToken);

        return Ok(result);
    }
}
