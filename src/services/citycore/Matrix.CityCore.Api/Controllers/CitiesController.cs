using Matrix.CityCore.Application.UseCases.Cities.ArchiveCity;
using Matrix.CityCore.Application.UseCases.Cities.Common;
using Matrix.CityCore.Application.UseCases.Cities.CreateCity;
using Matrix.CityCore.Application.UseCases.Cities.DeleteCity;
using Matrix.CityCore.Application.UseCases.Cities.GetCity;
using Matrix.CityCore.Application.UseCases.Cities.GetGenerationCatalog;
using Matrix.CityCore.Application.UseCases.Cities.GetSuggestedCityNames;
using Matrix.CityCore.Application.UseCases.Cities.ListCities;
using Matrix.CityCore.Application.UseCases.Cities.RenameCity;
using Matrix.CityCore.Application.UseCases.Cities.UpdateCityEnvironment;
using Matrix.CityCore.Application.UseCases.Topology.GetCityDistricts;
using Matrix.CityCore.Application.UseCases.Topology.GetCityResidentialBuildings;
using Matrix.CityCore.Application.UseCases.Weather.GetWeather;
using Matrix.CityCore.Contracts.Cities.Requests;
using Matrix.CityCore.Contracts.Cities.Views;
using Matrix.CityCore.Contracts.Generation.Views;
using Matrix.CityCore.Contracts.Topology.Views;
using Matrix.CityCore.Contracts.Weather.Views;
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
                    ClimateZone: request.ClimateZone,
                    Hemisphere: request.Hemisphere,
                    UtcOffsetMinutes: request.UtcOffsetMinutes,
                    GenerationSeed: request.GenerationSeed,
                    SizeTier: request.SizeTier,
                    UrbanDensity: request.UrbanDensity,
                    DevelopmentLevel: request.DevelopmentLevel,
                    StartSimTimeUtc: request.StartSimTimeUtc,
                    SpeedMultiplier: request.SpeedMultiplier),
                cancellationToken: cancellationToken);

            return Results.Created(
                uri: $"/api/cities/{cityId}",
                value: new CityCreatedView(CityId: cityId));
        }

        [HttpGet("generation/catalog")]
        public async Task<IResult> GetGenerationCatalog(CancellationToken cancellationToken)
        {
            CityGenerationCatalogDto catalog = await mediator.Send(
                request: new GetGenerationCatalogQuery(),
                cancellationToken: cancellationToken);

            return Results.Ok(
                new CityGenerationCatalogView(
                    CityNamePresets: catalog.CityNamePresets.ToArray(),
                    DistrictNamePresets: catalog.DistrictNamePresets.ToArray(),
                    StreetNamePresets: catalog.StreetNamePresets.ToArray()));
        }

        [HttpGet("generation/city-name-suggestions")]
        public async Task<IResult> GetSuggestedCityNames(
            [FromQuery] string? seed,
            [FromQuery] int count = 12,
            CancellationToken cancellationToken = default)
        {
            SuggestedCityNamesDto suggestions = await mediator.Send(
                request: new GetSuggestedCityNamesQuery(
                    Seed: seed,
                    Count: count),
                cancellationToken: cancellationToken);

            return Results.Ok(
                new SuggestedCityNamesView(
                    Seed: suggestions.Seed,
                    Names: suggestions.Names.ToArray()));
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

        [HttpGet("{cityId:guid}/districts")]
        public async Task<IResult> GetDistricts(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<DistrictDto> districts = await mediator.Send(
                request: new GetCityDistrictsQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            DistrictView[] views = districts
               .Select(MapToDistrictView)
               .ToArray();

            return Results.Ok(views);
        }

        [HttpGet("{cityId:guid}/residential-buildings")]
        public async Task<IResult> GetResidentialBuildings(
            [FromRoute] Guid cityId,
            [FromQuery] Guid? districtId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ResidentialBuildingDto> buildings = await mediator.Send(
                request: new GetCityResidentialBuildingsQuery(
                    CityId: cityId,
                    DistrictId: districtId),
                cancellationToken: cancellationToken);

            ResidentialBuildingView[] views = buildings
               .Select(MapToResidentialBuildingView)
               .ToArray();

            return Results.Ok(views);
        }

        [HttpGet("{cityId:guid}/weather")]
        public async Task<IResult> GetWeather(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityWeatherDto? weather = await mediator.Send(
                request: new GetWeatherQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            if (weather is null)
                return Results.NotFound();

            return Results.Ok(MapToWeatherView(weather));
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

        [HttpPut("{cityId:guid}/environment")]
        public async Task<IResult> UpdateEnvironment(
            [FromRoute] Guid cityId,
            [FromBody] UpdateCityEnvironmentRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new UpdateCityEnvironmentCommand(
                    CityId: cityId,
                    ClimateZone: request.ClimateZone,
                    Hemisphere: request.Hemisphere,
                    UtcOffsetMinutes: request.UtcOffsetMinutes),
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
                        message = "City must be archived before deletion."
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
                ClimateZone: dto.ClimateZone,
                Hemisphere: dto.Hemisphere,
                UtcOffsetMinutes: dto.UtcOffsetMinutes,
                GenerationSeed: dto.GenerationSeed,
                SizeTier: dto.SizeTier,
                UrbanDensity: dto.UrbanDensity,
                DevelopmentLevel: dto.DevelopmentLevel,
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

        private static DistrictView MapToDistrictView(DistrictDto dto)
        {
            return new DistrictView(
                DistrictId: dto.DistrictId,
                CityId: dto.CityId,
                Name: dto.Name,
                CreatedAtUtc: dto.CreatedAtUtc);
        }

        private static ResidentialBuildingView MapToResidentialBuildingView(ResidentialBuildingDto dto)
        {
            return new ResidentialBuildingView(
                ResidentialBuildingId: dto.ResidentialBuildingId,
                CityId: dto.CityId,
                DistrictId: dto.DistrictId,
                Name: dto.Name,
                Type: dto.Type,
                ResidentCapacity: dto.ResidentCapacity,
                CreatedAtUtc: dto.CreatedAtUtc);
        }

        private static CityWeatherView MapToWeatherView(CityWeatherDto dto)
        {
            return new CityWeatherView(
                CityId: dto.CityId,
                ClimateZone: dto.ClimateZone,
                CurrentType: dto.CurrentType,
                Severity: dto.Severity,
                PrecipitationKind: dto.PrecipitationKind,
                TemperatureC: dto.TemperatureC,
                HumidityPercent: dto.HumidityPercent,
                WindSpeedKph: dto.WindSpeedKph,
                CloudCoveragePercent: dto.CloudCoveragePercent,
                PressureHpa: dto.PressureHpa,
                StartedAtUtc: dto.StartedAtUtc,
                ExpectedUntilUtc: dto.ExpectedUntilUtc,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                LastTransitionAtUtc: dto.LastTransitionAtUtc,
                ActiveOverride: dto.ActiveOverride is null
                    ? null
                    : new CityWeatherOverrideView(
                        OverrideId: dto.ActiveOverride.OverrideId,
                        Source: dto.ActiveOverride.Source,
                        Reason: dto.ActiveOverride.Reason,
                        ForcedType: dto.ActiveOverride.ForcedType,
                        ForcedSeverity: dto.ActiveOverride.ForcedSeverity,
                        ForcedPrecipitationKind: dto.ActiveOverride.ForcedPrecipitationKind,
                        StartsAtUtc: dto.ActiveOverride.StartsAtUtc,
                        EndsAtUtc: dto.ActiveOverride.EndsAtUtc));
        }
    }
}