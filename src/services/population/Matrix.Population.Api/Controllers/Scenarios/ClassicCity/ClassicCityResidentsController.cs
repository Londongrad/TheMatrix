using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentsPage;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers.Scenarios.ClassicCity;

[ApiController]
[Authorize]
[Route(ClassicCityPopulationApiRoutes.ResidentsRoute)]
public sealed class ClassicCityResidentsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PersonDto>>> GetCityResidentsPage(
        [FromRoute] Guid cityId,
        [FromQuery] DateOnly currentDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var pagination = new Pagination(
            pageNumber: pageNumber,
            pageSize: pageSize);

        PagedResult<PersonDto> result = await _sender.Send(
            request: new GetCityResidentsPageQuery(
                CityId: cityId,
                CurrentDate: currentDate,
                Pagination: pagination),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpGet("{personId:guid}")]
    public async Task<ActionResult<CityResidentDetailsDto>> GetCityResidentDetails(
        [FromRoute] Guid cityId,
        [FromRoute] Guid personId,
        [FromQuery] DateOnly currentDate,
        CancellationToken cancellationToken = default)
    {
        CityResidentDetailsDto result = await _sender.Send(
            request: new GetCityResidentDetailsQuery(
                CityId: cityId,
                PersonId: personId,
                CurrentDate: currentDate),
            cancellationToken: cancellationToken);

        return Ok(result);
    }
}
