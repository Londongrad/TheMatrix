using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ArchiveCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateProvisionedCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.DeleteCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListCities;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListProvisioningCities;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RenameCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RetryCityPopulationBootstrapProvisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Weather.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationCore.Api.Controllers.Scenarios.ClassicCity
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
                    ClimateZone: request.ClimateZone,
                    Hemisphere: request.Hemisphere,
                    UtcOffsetMinutes: request.UtcOffsetMinutes,
                    GenerationSeed: request.GenerationSeed,
                    SizeTier: request.SizeTier,
                    UrbanDensity: request.UrbanDensity,
                    DevelopmentLevel: request.DevelopmentLevel,
                    EconomyProfile: request.EconomyProfile,
                    PopulationOccupancyProfile: request.PopulationOccupancyProfile,
                    InitialWeatherMode: request.InitialWeatherMode,
                    InitialWeatherType: request.InitialWeatherType,
                    InitialWeatherSeverity: request.InitialWeatherSeverity,
                    InitialWeatherTemperatureC: request.InitialWeatherTemperatureC,
                    ScenarioModelSetVersion: request.ScenarioModelSetVersion,
                    StartSimTimeUtc: request.StartSimTimeUtc,
                    SpeedMultiplier: request.SpeedMultiplier,
                    PlannedPeopleCount: request.PlannedPeopleCount,
                    ProvisioningCorrelationId: request.ProvisioningCorrelationId),
                cancellationToken: cancellationToken);

            return Results.Created(
                uri: $"/api/cities/{created.CityId}",
                value: new CityCreatedView(
                    CityId: created.CityId,
                    PopulationBootstrapOperationId: created.PopulationBootstrapOperationId,
                    EconomyBootstrapOperationId: created.EconomyBootstrapOperationId));
        }

        [HttpPost("provisioning")]
        public async Task<IResult> CreateProvisioned(
            [FromBody] CreateCityRequest request,
            CancellationToken cancellationToken)
        {
            CityProvisioningModel provisioning = await mediator.Send(
                request: new CreateProvisionedCityCommand(
                    City: new CreateCityCommand(
                        Name: request.Name,
                        ClimateZone: request.ClimateZone,
                        Hemisphere: request.Hemisphere,
                        UtcOffsetMinutes: request.UtcOffsetMinutes,
                        GenerationSeed: request.GenerationSeed,
                        SizeTier: request.SizeTier,
                        UrbanDensity: request.UrbanDensity,
                        DevelopmentLevel: request.DevelopmentLevel,
                        EconomyProfile: request.EconomyProfile,
                        PopulationOccupancyProfile: request.PopulationOccupancyProfile,
                        InitialWeatherMode: request.InitialWeatherMode,
                        InitialWeatherType: request.InitialWeatherType,
                        InitialWeatherSeverity: request.InitialWeatherSeverity,
                        InitialWeatherTemperatureC: request.InitialWeatherTemperatureC,
                        ScenarioModelSetVersion: request.ScenarioModelSetVersion,
                        StartSimTimeUtc: request.StartSimTimeUtc,
                        SpeedMultiplier: request.SpeedMultiplier,
                        PlannedPeopleCount: request.PlannedPeopleCount,
                        ProvisioningCorrelationId: request.ProvisioningCorrelationId)),
                cancellationToken: cancellationToken);

            return Results.Created(
                uri: $"/api/cities/{provisioning.CityId}",
                value: MapToProvisioningView(provisioning));
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

        [HttpGet("provisioning")]
        public async Task<IResult> ListProvisioning(CancellationToken cancellationToken)
        {
            IReadOnlyList<CityDto> cities = await mediator.Send(
                request: new ListProvisioningCitiesQuery(),
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

        [HttpGet("{cityId:guid}/anchors")]
        public async Task<IResult> GetAnchors(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityAnchorDto> anchors = await mediator.Send(
                request: new GetCityAnchorsQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            CityAnchorView[] views = anchors
               .Select(MapToCityAnchorView)
               .ToArray();

            return Results.Ok(views);
        }

        [HttpGet("{cityId:guid}/map")]
        public async Task<IResult> GetMap(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityMapTopologyDto map = await mediator.Send(
                request: new GetCityMapTopologyQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return Results.Ok(
                new CityMapTopologyView(
                    CityId: map.CityId,
                    Districts: map.Districts
                       .Select(MapToDistrictView)
                       .ToArray(),
                    ResidentialBuildings: map.ResidentialBuildings
                       .Select(MapToResidentialBuildingView)
                       .ToArray(),
                    Anchors: map.Anchors
                       .Select(MapToCityAnchorView)
                       .ToArray(),
                    RoadNodes: map.RoadNodes
                       .Select(MapToRoadNodeView)
                       .ToArray(),
                    RoadSegments: map.RoadSegments
                       .Select(MapToRoadSegmentView)
                       .ToArray()));
        }

        [HttpGet("{cityId:guid}/road-graph")]
        public async Task<IResult> GetRoadGraph(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityRoadGraphDto graph = await mediator.Send(
                request: new GetCityRoadGraphQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return Results.Ok(
                new CityRoadGraphView(
                    CityId: graph.CityId,
                    Districts: graph.Districts
                       .Select(MapToDistrictView)
                       .ToArray(),
                    RoadSegments: graph.RoadSegments
                       .Select(MapToRoadSegmentView)
                       .ToArray()));
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
                        EconomyBootstrapOperationId: result.EconomyBootstrapOperationId!.Value)),
                RestartCityPopulationBootstrapStatus.NotFound => Results.NotFound(),
                RestartCityPopulationBootstrapStatus.NotAllowed => Results.Conflict(
                    new
                    {
                        code = "SimulationCore.City.PopulationBootstrapRetryNotAllowed",
                        message = "Population bootstrap retry is allowed only after a failed bootstrap attempt."
                    }),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        [HttpPost("{cityId:guid}/population-bootstrap/retry-provisioning")]
        public async Task<IResult> RetryPopulationBootstrapProvisioning(
            [FromRoute] Guid cityId,
            [FromBody] RetryCityPopulationBootstrapProvisioningRequest? request,
            CancellationToken cancellationToken)
        {
            RetryCityPopulationBootstrapProvisioningResult result = await mediator.Send(
                request: new RetryCityPopulationBootstrapProvisioningCommand(
                    CityId: cityId,
                    PlannedPeopleCountOverride: request?.PlannedPeopleCountOverride),
                cancellationToken: cancellationToken);

            return result.Status switch
            {
                RetryCityPopulationBootstrapProvisioningStatus.Accepted => Results.Ok(
                    MapToProvisioningView(result.Provisioning!)),
                RetryCityPopulationBootstrapProvisioningStatus.NotFound => Results.NotFound(),
                RetryCityPopulationBootstrapProvisioningStatus.NotAllowed => Results.Conflict(
                    new
                    {
                        code = "SimulationCore.City.PopulationBootstrapRetryNotAllowed",
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
                request: new CompleteCityPopulationBootstrapEndpointCommand(
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
                request: new FailCityPopulationBootstrapEndpointCommand(
                    CityId: cityId,
                    OperationId: request.OperationId,
                    FailureCode: request.FailureCode),
                cancellationToken: cancellationToken);

            return updated
                ? Results.NoContent()
                : Results.NotFound();
        }

        [HttpPost("{cityId:guid}/economy-bootstrap/complete")]
        public async Task<IResult> CompleteEconomyBootstrap(
            [FromRoute] Guid cityId,
            [FromBody] CompleteCityEconomyBootstrapRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new CompleteCityEconomyBootstrapEndpointCommand(
                    CityId: cityId,
                    OperationId: request.OperationId),
                cancellationToken: cancellationToken);

            return updated
                ? Results.NoContent()
                : Results.NotFound();
        }

        [HttpPost("{cityId:guid}/economy-bootstrap/fail")]
        public async Task<IResult> FailEconomyBootstrap(
            [FromRoute] Guid cityId,
            [FromBody] FailCityEconomyBootstrapRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await mediator.Send(
                request: new FailCityEconomyBootstrapEndpointCommand(
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
                        code = "SimulationCore.City.DeleteNotAllowed",
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
                Status: dto.Status,
                ClimateZone: dto.ClimateZone,
                Hemisphere: dto.Hemisphere,
                UtcOffsetMinutes: dto.UtcOffsetMinutes,
                GenerationSeed: dto.GenerationSeed,
                RunMetadata: new CityRunMetadataView(
                    RunId: dto.RunId,
                    SimulationSeed: dto.GenerationSeed,
                    ScenarioModelSetVersion: dto.ScenarioModelSetVersion),
                SizeTier: dto.SizeTier,
                UrbanDensity: dto.UrbanDensity,
                DevelopmentLevel: dto.DevelopmentLevel,
                EconomyProfile: dto.EconomyProfile,
                PopulationOccupancyProfile: dto.PopulationOccupancyProfile,
                CreatedAtUtc: dto.CreatedAtUtc,
                ArchivedAtUtc: dto.ArchivedAtUtc,
                PlannedPeopleCount: dto.PlannedPeopleCount);
        }

        private static CityProvisioningStatusView MapToProvisioningStatusView(CityDto dto)
        {
            return new CityProvisioningStatusView(
                CityId: dto.CityId,
                Status: dto.Status,
                PopulationBootstrapOperationId: dto.PopulationBootstrapOperationId,
                EconomyBootstrapOperationId: dto.EconomyBootstrapOperationId,
                PopulationBootstrapFailureCode: dto.PopulationBootstrapFailureCode,
                EconomyBootstrapFailureCode: dto.EconomyBootstrapFailureCode,
                PopulationBootstrapCompletedAtUtc: dto.PopulationBootstrapCompletedAtUtc,
                EconomyBootstrapCompletedAtUtc: dto.EconomyBootstrapCompletedAtUtc,
                PopulationBootstrapFailedAtUtc: dto.PopulationBootstrapFailedAtUtc,
                EconomyBootstrapFailedAtUtc: dto.EconomyBootstrapFailedAtUtc,
                ProvisioningStartedAtUtc: dto.ProvisioningStartedAtUtc,
                ProvisioningHeartbeatAtUtc: dto.ProvisioningHeartbeatAtUtc,
                ProvisioningLeaseExpiresAtUtc: dto.ProvisioningLeaseExpiresAtUtc,
                ProvisioningAttemptCount: dto.ProvisioningAttemptCount);
        }

        private static CityProvisioningView MapToProvisioningView(CityProvisioningModel model)
        {
            return new CityProvisioningView(
                CityId: model.CityId,
                PopulationBootstrap: MapToPopulationBootstrapView(model.PopulationBootstrap),
                EconomyBootstrap: MapToEconomyBootstrapView(model.EconomyBootstrap));
        }

        private static CityPopulationBootstrapView MapToPopulationBootstrapView(CityPopulationBootstrapModel model)
        {
            return new CityPopulationBootstrapView(
                OperationId: model.OperationId,
                Status: model.Status,
                PlannedPeopleCount: model.PlannedPeopleCount,
                ResidentialCapacity: model.ResidentialCapacity,
                Summary: model.Summary is null
                    ? null
                    : MapToPopulationBootstrapSummaryView(model.Summary),
                FailureCode: model.FailureCode);
        }

        private static CityPopulationBootstrapSummaryView MapToPopulationBootstrapSummaryView(
            CityPopulationBootstrapSummaryModel model)
        {
            return new CityPopulationBootstrapSummaryView(
                CityId: model.CityId,
                RequestedPeopleCount: model.RequestedPeopleCount,
                GeneratedPeopleCount: model.GeneratedPeopleCount,
                HouseholdCount: model.HouseholdCount,
                HousedHouseholdCount: model.HousedHouseholdCount,
                HomelessHouseholdCount: model.HomelessHouseholdCount,
                HousedPeopleCount: model.HousedPeopleCount,
                HomelessPeopleCount: model.HomelessPeopleCount);
        }

        private static CityEconomyBootstrapView MapToEconomyBootstrapView(CityEconomyBootstrapModel model)
        {
            return new CityEconomyBootstrapView(
                OperationId: model.OperationId,
                Status: model.Status,
                FailureCode: model.FailureCode,
                UnitKind: model.UnitKind,
                UnitCode: model.UnitCode,
                UnitDisplayName: model.UnitDisplayName,
                UnitSymbol: model.UnitSymbol);
        }

        private static CityListItemView MapToListItemView(CityDto dto)
        {
            return new CityListItemView(
                CityId: dto.CityId,
                SimulationId: dto.SimulationId,
                Name: dto.Name,
                Status: dto.Status,
                CreatedAtUtc: dto.CreatedAtUtc,
                PopulationBootstrapCompletedAtUtc: dto.PopulationBootstrapCompletedAtUtc,
                PopulationBootstrapFailedAtUtc: dto.PopulationBootstrapFailedAtUtc,
                PopulationBootstrapFailureCode: dto.PopulationBootstrapFailureCode,
                ArchivedAtUtc: dto.ArchivedAtUtc);
        }

        private static DistrictView MapToDistrictView(DistrictDto dto)
        {
            return new DistrictView(
                DistrictId: dto.DistrictId,
                CityId: dto.CityId,
                Name: dto.Name,
                AnchorX: dto.AnchorX,
                AnchorY: dto.AnchorY,
                CreatedAtUtc: dto.CreatedAtUtc);
        }

        private static ResidentialBuildingView MapToResidentialBuildingView(ResidentialBuildingDto dto)
        {
            return new ResidentialBuildingView(
                ResidentialBuildingId: dto.ResidentialBuildingId,
                CityId: dto.CityId,
                DistrictId: dto.DistrictId,
                AccessRoadNodeId: dto.AccessRoadNodeId,
                Name: dto.Name,
                Type: dto.Type,
                ResidentCapacity: dto.ResidentCapacity,
                PositionX: dto.PositionX,
                PositionY: dto.PositionY,
                CreatedAtUtc: dto.CreatedAtUtc);
        }

        private static RoadNodeView MapToRoadNodeView(RoadNodeDto dto)
        {
            return new RoadNodeView(
                RoadNodeId: dto.RoadNodeId,
                CityId: dto.CityId,
                DistrictId: dto.DistrictId,
                Name: dto.Name,
                Type: dto.Type,
                PositionX: dto.PositionX,
                PositionY: dto.PositionY,
                CreatedAtUtc: dto.CreatedAtUtc);
        }

        private static CityAnchorView MapToCityAnchorView(CityAnchorDto dto)
        {
            return new CityAnchorView(
                CityAnchorId: dto.CityAnchorId,
                CityId: dto.CityId,
                DistrictId: dto.DistrictId,
                AccessRoadNodeId: dto.AccessRoadNodeId,
                Name: dto.Name,
                Type: dto.Type,
                Capacity: dto.Capacity,
                PositionX: dto.PositionX,
                PositionY: dto.PositionY,
                CreatedAtUtc: dto.CreatedAtUtc);
        }

        private static RoadSegmentView MapToRoadSegmentView(RoadSegmentDto dto)
        {
            return new RoadSegmentView(
                RoadSegmentId: dto.RoadSegmentId,
                CityId: dto.CityId,
                DistrictId: dto.DistrictId,
                FromRoadNodeId: dto.FromRoadNodeId,
                ToRoadNodeId: dto.ToRoadNodeId,
                Name: dto.Name,
                Type: dto.Type,
                LengthMeters: dto.LengthMeters,
                CreatedAtUtc: dto.CreatedAtUtc);
        }

        private static CityWeatherView MapToWeatherView(CityWeatherDto dto)
        {
            return new CityWeatherView(
                CityId: dto.CityId,
                ClimateZone: dto.ClimateZone,
                Hemisphere: dto.Hemisphere,
                UtcOffsetMinutes: dto.UtcOffsetMinutes,
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
