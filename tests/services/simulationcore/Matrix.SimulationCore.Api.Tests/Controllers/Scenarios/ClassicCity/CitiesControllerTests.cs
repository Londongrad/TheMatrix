using Matrix.SimulationCore.Api.Controllers.Scenarios.ClassicCity;
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
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds;
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
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationCore.Api.Tests.TestSupport.SimulationCoreApiTestSupport;

namespace Matrix.SimulationCore.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class CitiesControllerTests
    {
        [Fact]
        public async Task CreateEndpoints_ReturnCreatedViewsAndForwardPayloads()
        {
            CreateCityRequest request = CreateCreateCityRequest();
            CityCreatedDto created = CreateCityCreatedDto();
            CityProvisioningModel provisioning = CreateProvisioningModel(created.CityId);
            var sender = new FakeSender();
            sender.Handle<CreateCityCommand, CityCreatedDto>(command =>
            {
                Assert.Equal(
                    expected: request.Name,
                    actual: command.Name);
                Assert.Equal(
                    expected: request.PlannedPeopleCount,
                    actual: command.PlannedPeopleCount);
                Assert.Equal(
                    expected: request.SpeedMultiplier,
                    actual: command.SpeedMultiplier);
                return created;
            });
            sender.Handle<CreateProvisionedCityCommand, CityProvisioningModel>(command =>
            {
                Assert.Equal(
                    expected: request.Name,
                    actual: command.City.Name);
                Assert.Equal(
                    expected: request.ProvisioningCorrelationId,
                    actual: command.City.ProvisioningCorrelationId);
                return provisioning;
            });
            var controller = new CitiesController(sender);

            IResult create = await controller.Create(
                request: request,
                cancellationToken: CancellationToken.None);
            IResult createProvisioned = await controller.CreateProvisioned(
                request: request,
                cancellationToken: CancellationToken.None);

            CityCreatedView createdView = AssertResult<CityCreatedView>(
                result: create,
                expectedStatusCode: StatusCodes.Status201Created);
            Assert.Equal(
                expected: created.CityId,
                actual: createdView.CityId);
            Assert.Equal(
                expected: created.SimulationKind,
                actual: createdView.SimulationKind);

            CityProvisioningView provisioningView = AssertResult<CityProvisioningView>(
                result: createProvisioned,
                expectedStatusCode: StatusCodes.Status201Created);
            Assert.Equal(
                expected: provisioning.CityId,
                actual: provisioningView.CityId);
            Assert.Equal(
                expected: "Completed",
                actual: provisioningView.PopulationBootstrap.Status);
            Assert.Equal(
                expected: "CRD",
                actual: provisioningView.EconomyBootstrap.UnitCode);
        }

        [Fact]
        public async Task CatalogAndLookupQueries_ReturnMappedViews()
        {
            var cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
            CityDto city = CreateCityDto(
                cityId: cityId,
                status: "Provisioning");
            CityWeatherDto weather = CreateWeatherDto(cityId);
            var sender = new FakeSender();
            sender.Handle<GetGenerationCatalogQuery, CityGenerationCatalogDto>(_ => CreateGenerationCatalogDto());
            sender.Handle<GetSimulationKindsQuery, IReadOnlyList<SimulationKindCatalogItemDto>>(_
                => CreateSimulationKinds());
            sender.Handle<GetSuggestedCityNamesQuery, SuggestedCityNamesDto>(query =>
            {
                Assert.Equal(
                    expected: "alpha",
                    actual: query.Seed);
                Assert.Equal(
                    expected: 5,
                    actual: query.Count);
                return CreateSuggestedCityNamesDto("alpha");
            });
            sender.Handle<ListCitiesQuery, IReadOnlyList<CityDto>>(query =>
            {
                Assert.True(query.IncludeArchived);
                return [city];
            });
            sender.Handle<ListProvisioningCitiesQuery, IReadOnlyList<CityDto>>(_ => [city]);
            sender.Handle<GetCityQuery, CityDto?>(_ => city);
            sender.Handle<GetWeatherQuery, CityWeatherDto?>(_ => weather);
            var controller = new CitiesController(sender);

            IResult generationCatalog = await controller.GetGenerationCatalog(CancellationToken.None);
            IResult simulationKinds = await controller.GetSimulationKinds(CancellationToken.None);
            IResult suggestions = await controller.GetSuggestedCityNames(
                seed: "alpha",
                count: 5,
                cancellationToken: CancellationToken.None);
            IResult list = await controller.List(
                includeArchived: true,
                cancellationToken: CancellationToken.None);
            IResult listProvisioning = await controller.ListProvisioning(CancellationToken.None);
            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult getProvisioning = await controller.GetProvisioning(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult getWeather = await controller.GetWeather(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            CityGenerationCatalogView catalogView = AssertResult<CityGenerationCatalogView>(
                result: generationCatalog,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expectedSpan:
                [
                    "Mega City",
                    "Neo Harbor"
                ],
                actualArray: catalogView.CityNamePresets);

            SimulationKindCatalogItemView[] kindsView = AssertResult<SimulationKindCatalogItemView[]>(
                result: simulationKinds,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Single(kindsView);
            Assert.True(kindsView[0].IsDefault);

            SuggestedCityNamesView suggestionsView = AssertResult<SuggestedCityNamesView>(
                result: suggestions,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: "alpha",
                actual: suggestionsView.Seed);
            Assert.Equal(
                expected: 3,
                actual: suggestionsView.Names.Length);

            CityListItemView[] listed = AssertResult<CityListItemView[]>(
                result: list,
                expectedStatusCode: StatusCodes.Status200OK);
            CityListItemView[] provisioningListed = AssertResult<CityListItemView[]>(
                result: listProvisioning,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Single(listed);
            Assert.Single(provisioningListed);

            CityView cityView = AssertResult<CityView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: cityId,
                actual: cityView.CityId);
            Assert.Equal(
                expected: "Mega City",
                actual: cityView.Name);

            CityProvisioningStatusView provisioningStatus = AssertResult<CityProvisioningStatusView>(
                result: getProvisioning,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: provisioningStatus.PopulationBootstrapOperationId);

            CityWeatherView weatherView = AssertResult<CityWeatherView>(
                result: getWeather,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: "Clear",
                actual: weatherView.CurrentType);
            Assert.Equal(
                expected: 18.5m,
                actual: weatherView.TemperatureC);
        }

        [Fact]
        public async Task TopologyAndWeatherQueries_MapProjectedViewsAndMissingWeather()
        {
            var cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
            CityMapTopologyDto map = CreateMapTopologyDto(cityId);
            CityRoadGraphDto roadGraph = CreateRoadGraphDto(cityId);
            DistrictDto district = map.Districts[0];
            ResidentialBuildingDto building = map.ResidentialBuildings[0];
            CityAnchorDto anchor = map.Anchors[0];
            var sender = new FakeSender();
            sender.Handle<GetCityDistrictsQuery, IReadOnlyList<DistrictDto>>(_ => [district]);
            sender.Handle<GetCityResidentialBuildingsQuery, IReadOnlyList<ResidentialBuildingDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                Assert.Equal(
                    expected: district.DistrictId,
                    actual: query.DistrictId);
                return [building];
            });
            sender.Handle<GetCityAnchorsQuery, IReadOnlyList<CityAnchorDto>>(_ => [anchor]);
            sender.Handle<GetCityMapTopologyQuery, CityMapTopologyDto>(_ => map);
            sender.Handle<GetCityRoadGraphQuery, CityRoadGraphDto>(_ => roadGraph);
            sender.Handle<GetWeatherQuery, CityWeatherDto?>(_ => null);
            var controller = new CitiesController(sender);

            IResult districts = await controller.GetDistricts(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult buildings = await controller.GetResidentialBuildings(
                cityId: cityId,
                districtId: district.DistrictId,
                cancellationToken: CancellationToken.None);
            IResult anchors = await controller.GetAnchors(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult getMap = await controller.GetMap(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult getRoadGraph = await controller.GetRoadGraph(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult missingWeather = await controller.GetWeather(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            DistrictView[] districtViews = AssertResult<DistrictView[]>(
                result: districts,
                expectedStatusCode: StatusCodes.Status200OK);
            ResidentialBuildingView[] buildingViews = AssertResult<ResidentialBuildingView[]>(
                result: buildings,
                expectedStatusCode: StatusCodes.Status200OK);
            CityAnchorView[] anchorViews = AssertResult<CityAnchorView[]>(
                result: anchors,
                expectedStatusCode: StatusCodes.Status200OK);
            CityMapTopologyView mapView = AssertResult<CityMapTopologyView>(
                result: getMap,
                expectedStatusCode: StatusCodes.Status200OK);
            CityRoadGraphView graphView = AssertResult<CityRoadGraphView>(
                result: getRoadGraph,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: district.Name,
                actual: Assert.Single(districtViews)
                   .Name);
            Assert.Equal(
                expected: building.Name,
                actual: Assert.Single(buildingViews)
                   .Name);
            Assert.Equal(
                expected: anchor.Name,
                actual: Assert.Single(anchorViews)
                   .Name);
            Assert.Single(mapView.RoadNodes);
            Assert.Single(graphView.RoadSegments);
            AssertStatus(
                result: missingWeather,
                expectedStatusCode: StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task BootstrapAndLifecycleMutations_MapStatusesAndBodies()
        {
            var cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
            var populationOperationId = Guid.Parse("f91e16fd-ee76-4330-8dda-3fb5b8749d52");
            var economyOperationId = Guid.Parse("2c03f709-08b6-4d6c-8789-f45f5ecdd3a3");
            CityProvisioningModel provisioning = CreateProvisioningModel(cityId);
            var sender = new FakeSender();
            sender.Handle<RestartCityPopulationBootstrapCommand, RestartCityPopulationBootstrapResult>(_ =>
                RestartCityPopulationBootstrapResult.Restarted(
                    populationOperationId: populationOperationId,
                    economyOperationId: economyOperationId,
                    simulationKind: "ClassicCity"));
            sender
               .Handle<RetryCityPopulationBootstrapProvisioningCommand,
                    RetryCityPopulationBootstrapProvisioningResult>(command =>
                {
                    Assert.Equal(
                        expected: cityId,
                        actual: command.CityId);
                    Assert.Equal(
                        expected: 144,
                        actual: command.PlannedPeopleCountOverride);
                    return RetryCityPopulationBootstrapProvisioningResult.Accepted(provisioning);
                });
            sender.Handle<CompleteCityPopulationBootstrapEndpointCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: populationOperationId,
                    actual: command.OperationId);
                return true;
            });
            sender.Handle<FailCityPopulationBootstrapEndpointCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: populationOperationId,
                    actual: command.OperationId);
                Assert.Equal(
                    expected: "Population.SeedInvalid",
                    actual: command.FailureCode);
                return false;
            });
            sender.Handle<CompleteCityEconomyBootstrapEndpointCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: economyOperationId,
                    actual: command.OperationId);
                return true;
            });
            sender.Handle<FailCityEconomyBootstrapEndpointCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: economyOperationId,
                    actual: command.OperationId);
                Assert.Equal(
                    expected: "Economy.UnitInvalid",
                    actual: command.FailureCode);
                return true;
            });
            sender.Handle<RenameCityCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: "Neo City",
                    actual: command.Name);
                return true;
            });
            sender.Handle<UpdateCityEnvironmentCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: "Arid",
                    actual: command.ClimateZone);
                Assert.Equal(
                    expected: "Southern",
                    actual: command.Hemisphere);
                Assert.Equal(
                    expected: -120,
                    actual: command.UtcOffsetMinutes);
                return true;
            });
            sender.Handle<ArchiveCityCommand, bool>(_ => true);
            sender.Handle<DeleteCityCommand, DeleteCityResult>(_ => DeleteCityResult.NotAllowed);
            var controller = new CitiesController(sender);

            IResult retry = await controller.RetryPopulationBootstrap(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult retryProvisioning = await controller.RetryPopulationBootstrapProvisioning(
                cityId: cityId,
                request: new RetryCityPopulationBootstrapProvisioningRequest(144),
                cancellationToken: CancellationToken.None);
            IResult completePopulation = await controller.CompletePopulationBootstrap(
                cityId: cityId,
                request: new CompleteCityPopulationBootstrapRequest(populationOperationId),
                cancellationToken: CancellationToken.None);
            IResult failPopulation = await controller.FailPopulationBootstrap(
                cityId: cityId,
                request: new FailCityPopulationBootstrapRequest(
                    OperationId: populationOperationId,
                    FailureCode: "Population.SeedInvalid"),
                cancellationToken: CancellationToken.None);
            IResult completeEconomy = await controller.CompleteEconomyBootstrap(
                cityId: cityId,
                request: new CompleteCityEconomyBootstrapRequest(economyOperationId),
                cancellationToken: CancellationToken.None);
            IResult failEconomy = await controller.FailEconomyBootstrap(
                cityId: cityId,
                request: new FailCityEconomyBootstrapRequest(
                    OperationId: economyOperationId,
                    FailureCode: "Economy.UnitInvalid"),
                cancellationToken: CancellationToken.None);
            IResult rename = await controller.Rename(
                cityId: cityId,
                request: new RenameCityRequest("Neo City"),
                cancellationToken: CancellationToken.None);
            IResult updateEnvironment = await controller.UpdateEnvironment(
                cityId: cityId,
                request: new UpdateCityEnvironmentRequest(
                    ClimateZone: "Arid",
                    Hemisphere: "Southern",
                    UtcOffsetMinutes: -120),
                cancellationToken: CancellationToken.None);
            IResult archive = await controller.Archive(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult delete = await controller.Delete(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            CityPopulationBootstrapRestartedView retryView =
                AssertResult<CityPopulationBootstrapRestartedView>(
                    result: retry,
                    expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: populationOperationId,
                actual: retryView.PopulationBootstrapOperationId);

            CityProvisioningView retryProvisioningView =
                AssertResult<CityProvisioningView>(
                    result: retryProvisioning,
                    expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: provisioning.CityId,
                actual: retryProvisioningView.CityId);

            AssertStatus(
                result: completePopulation,
                expectedStatusCode: StatusCodes.Status204NoContent);
            AssertStatus(
                result: failPopulation,
                expectedStatusCode: StatusCodes.Status404NotFound);
            AssertStatus(
                result: completeEconomy,
                expectedStatusCode: StatusCodes.Status204NoContent);
            AssertStatus(
                result: failEconomy,
                expectedStatusCode: StatusCodes.Status204NoContent);
            AssertStatus(
                result: rename,
                expectedStatusCode: StatusCodes.Status204NoContent);
            AssertStatus(
                result: updateEnvironment,
                expectedStatusCode: StatusCodes.Status204NoContent);
            AssertStatus(
                result: archive,
                expectedStatusCode: StatusCodes.Status204NoContent);
            Assert.Equal(
                expected: "SimulationCore.City.DeleteNotAllowed",
                actual: GetAnonymousProperty<string>(
                    result: delete,
                    propertyName: "code",
                    expectedStatusCode: StatusCodes.Status409Conflict));
        }
    }
}
