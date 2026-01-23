using Matrix.CityCore.Application.UseCases.Cities.ArchiveCity;
using Matrix.CityCore.Application.UseCases.Cities.Common;
using Matrix.CityCore.Application.UseCases.Cities.CreateCity;
using Matrix.CityCore.Application.UseCases.Cities.DeleteCity;
using Matrix.CityCore.Application.UseCases.Cities.GetCity;
using Matrix.CityCore.Application.UseCases.Cities.ListCities;
using Matrix.CityCore.Application.UseCases.Cities.RenameCity;
using Matrix.CityCore.Contracts.Cities.Requests;
using Matrix.CityCore.Contracts.Cities.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.CityCore.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cities")]
    public sealed class CitiesController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IResult> Create(
            [FromBody] CreateCityRequest request,
            CancellationToken cancellationToken)
        {
            Guid cityId = await mediator.Send(
                request: new CreateCityCommand(
                    Name: request.Name,
                    StartSimTimeUtc: request.StartSimTimeUtc,
                    SpeedMultiplier: request.SpeedMultiplier),
                cancellationToken: cancellationToken);

            return Results.Created(
                uri: $"/api/cities/{cityId}",
                value: new CityCreatedView(CityId: cityId));
        }

        [HttpGet]
        public async Task<IResult> List(
            [FromQuery] bool includeArchived,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityDto> cities = await mediator.Send(
                request: new ListCitiesQuery(IncludeArchived: includeArchived),
                cancellationToken: cancellationToken);

            CityListItemView[] views = cities
               .Select(MapToListItemView)
               .ToArray();

            return Results.Ok(views);
        }

        [HttpGet("{cityId:guid}")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityDto? city = await mediator.Send(
                request: new GetCityQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return Results.NotFound();

            return Results.Ok(MapToView(city));
        }

        [HttpPut("{cityId:guid}/name")]
        public async Task<IResult> Rename(
            [FromRoute] Guid cityId,
            [FromBody] RenameCityRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new RenameCityCommand(
                    CityId: cityId,
                    Name: request.Name),
                cancellationToken: cancellationToken);

            return updated
                ? Results.NoContent()
                : Results.NotFound();
        }

        [HttpPost("{cityId:guid}/archive")]
        public async Task<IResult> Archive(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new ArchiveCityCommand(CityId: cityId),
                cancellationToken: cancellationToken);

            return updated
                ? Results.NoContent()
                : Results.NotFound();
        }

        [HttpDelete("{cityId:guid}")]
        public async Task<IResult> Delete(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            DeleteCityResult result = await mediator.Send(
                request: new DeleteCityCommand(CityId: cityId),
                cancellationToken: cancellationToken);

            return result switch
            {
                DeleteCityResult.Deleted => Results.NoContent(),
                DeleteCityResult.NotFound => Results.NotFound(),
                DeleteCityResult.NotAllowed => Results.Conflict(
                    new
                    {
                        code = "CityCore.City.DeleteNotAllowed",
                        message = "City must be archived before deletion. Use force=true for test data removal."
                    }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        private static CityView MapToView(CityDto dto)
        {
            return new CityView(
                CityId: dto.CityId,
                Name: dto.Name,
                Status: dto.Status,
                CreatedAtUtc: dto.CreatedAtUtc,
                ArchivedAtUtc: dto.ArchivedAtUtc);
        }

        private static CityListItemView MapToListItemView(CityDto dto)
        {
            return new CityListItemView(
                CityId: dto.CityId,
                Name: dto.Name,
                Status: dto.Status);
        }
    }
}
