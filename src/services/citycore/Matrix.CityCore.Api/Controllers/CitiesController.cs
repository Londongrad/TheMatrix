using Matrix.CityCore.Application.UseCases.Cities.ArchiveCity;
using Matrix.CityCore.Application.UseCases.Cities.Common;
using Matrix.CityCore.Application.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.CityCore.Application.UseCases.Cities.CreateCity;
using Matrix.CityCore.Application.UseCases.Cities.DeleteCity;
using Matrix.CityCore.Application.UseCases.Cities.FailPopulationBootstrap;
using Matrix.CityCore.Application.UseCases.Cities.GetCity;
using Matrix.CityCore.Application.UseCases.Cities.GetGenerationCatalog;
using Matrix.CityCore.Application.UseCases.Cities.GetSimulationKinds;
using Matrix.CityCore.Application.UseCases.Cities.GetSuggestedCityNames;
using Matrix.CityCore.Application.UseCases.Cities.ListCities;
using Matrix.CityCore.Application.UseCases.Cities.RenameCity;
using Matrix.CityCore.Application.UseCases.Cities.RestartPopulationBootstrap;
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
            CityCreatedDto created = await mediator.Send(
                request: new CreateCityCommand(
                    Name: request.Name,
                    SimulationKind: request.SimulationKind,
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
                uri: $"/api/cities/{created.CityId}",
                value: new CityCreatedView(
                    CityId: created.CityId,
                    PopulationBootstrapOperationId: created.PopulationBootstrapOperationId,
                    SimulationKind: created.SimulationKind));
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

        [HttpGet("simulation-kinds")]
        public async Task<IResult> GetSimulationKinds(CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationKindCatalogItemDto> kinds = await mediator.Send(
                request: new GetSimulationKindsQuery(),
                cancellationToken: cancellationToken);

            SimulationKindCatalogItemView[] views = kinds
               .Select(kind => new SimulationKindCatalogItemView(
                    Kind: kind.Kind,
                    DisplayName: kind.DisplayName,
                    Description: kind.Description,
                    SupportsAutomaticPopulationBootstrap: kind.SupportsAutomaticPopulationBootstrap,
                    IsDefault: kind.IsDefault))
               .ToArray();

            return Results.Ok(views);
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

        [HttpGet("{cityId:guid}/provisioning")]
        public async Task<IResult> GetProvisioning(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityDto? city = await mediator.Send(
                request: new GetCityQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return Results.NotFound();

            return Results.Ok(MapToProvisioningStatusView(city));
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

        [HttpPost("{cityId:guid}/population-bootstrap/retry")]
        public async Task<IResult> RetryPopulationBootstrap(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            RestartCityPopulationBootstrapResult result = await mediator.Send(
                request: new RestartCityPopulationBootstrapCommand(CityId: cityId),
                cancellationToken: cancellationToken);

            return result.Status switch
            {
                RestartCityPopulationBootstrapStatus.Restarted => Results.Ok(
                    new CityPopulationBootstrapRestartedView(
                        CityId: cityId,
                        PopulationBootstrapOperationId: result.PopulationBootstrapOperationId!.Value,
                        SimulationKind: result.SimulationKind!)),
                RestartCityPopulationBootstrapStatus.NotFound => Results.NotFound(),
                RestartCityPopulationBootstrapStatus.NotAllowed => Results.Conflict(
                    new
                    {
                        code = "CityCore.City.PopulationBootstrapRetryNotAllowed",
                        message = "Population bootstrap retry is allowed only after a failed bootstrap attempt."
                    }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        [HttpPost("{cityId:guid}/population-bootstrap/complete")]
        public async Task<IResult> CompletePopulationBootstrap(
            [FromRoute] Guid cityId,
            [FromBody] CompleteCityPopulationBootstrapRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new CompleteCityPopulationBootstrapCommand(
                    CityId: cityId,
                    OperationId: request.OperationId),
                cancellationToken: cancellationToken);

            return updated
                ? Results.NoContent()
                : Results.NotFound();
        }

        [HttpPost("{cityId:guid}/population-bootstrap/fail")]
        public async Task<IResult> FailPopulationBootstrap(
            [FromRoute] Guid cityId,
            [FromBody] FailCityPopulationBootstrapRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new FailCityPopulationBootstrapCommand(
                    CityId: cityId,
                    OperationId: request.OperationId,
                    FailureCode: request.FailureCode),
                cancellationToken: cancellationToken);

            return updated
                ? Results.NoContent()
                : Results.NotFound();
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
                SimulationId: dto.SimulationId,
                Name: dto.Name,
                SimulationKind: dto.SimulationKind,
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

        private static CityProvisioningStatusView MapToProvisioningStatusView(CityDto dto)
        {
            return new CityProvisioningStatusView(
                CityId: dto.CityId,
                Status: dto.Status,
                PopulationBootstrapOperationId: dto.PopulationBootstrapOperationId,
                PopulationBootstrapFailureCode: dto.PopulationBootstrapFailureCode,
                PopulationBootstrapCompletedAtUtc: dto.PopulationBootstrapCompletedAtUtc,
                PopulationBootstrapFailedAtUtc: dto.PopulationBootstrapFailedAtUtc);
        }

        private static CityListItemView MapToListItemView(CityDto dto)
        {
            return new CityListItemView(
                CityId: dto.CityId,
                SimulationId: dto.SimulationId,
                Name: dto.Name,
                SimulationKind: dto.SimulationKind,
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
