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

namespace Matrix.SimulationCore.Api.Tests.Controllers.Scenarios.ClassicCity;

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
            Assert.Equal(request.Name, command.Name);
            Assert.Equal(request.PlannedPeopleCount, command.PlannedPeopleCount);
            Assert.Equal(request.SpeedMultiplier, command.SpeedMultiplier);
            return created;
        });
        sender.Handle<CreateProvisionedCityCommand, CityProvisioningModel>(command =>
        {
            Assert.Equal(request.Name, command.City.Name);
            Assert.Equal(request.SimulationKind, command.City.SimulationKind);
            Assert.Equal(request.ProvisioningCorrelationId, command.City.ProvisioningCorrelationId);
            return provisioning;
        });
        var controller = new CitiesController(sender);

        IResult create = await controller.Create(request, CancellationToken.None);
        IResult createProvisioned = await controller.CreateProvisioned(request, CancellationToken.None);

        CityCreatedView createdView = AssertResult<CityCreatedView>(create, StatusCodes.Status201Created);
        Assert.Equal(created.CityId, createdView.CityId);
        Assert.Equal(created.SimulationKind, createdView.SimulationKind);

        CityProvisioningView provisioningView = AssertResult<CityProvisioningView>(createProvisioned, StatusCodes.Status201Created);
        Assert.Equal(provisioning.CityId, provisioningView.CityId);
        Assert.Equal("Completed", provisioningView.PopulationBootstrap.Status);
        Assert.Equal("CRD", provisioningView.EconomyBootstrap.UnitCode);
    }

    [Fact]
    public async Task CatalogAndLookupQueries_ReturnMappedViews()
    {
        Guid cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
        CityDto city = CreateCityDto(cityId, status: "Provisioning");
        CityWeatherDto weather = CreateWeatherDto(cityId);
        var sender = new FakeSender();
        sender.Handle<GetGenerationCatalogQuery, CityGenerationCatalogDto>(_ => CreateGenerationCatalogDto());
        sender.Handle<GetSimulationKindsQuery, IReadOnlyList<SimulationKindCatalogItemDto>>(_ => CreateSimulationKinds());
        sender.Handle<GetSuggestedCityNamesQuery, SuggestedCityNamesDto>(query =>
        {
            Assert.Equal("alpha", query.Seed);
            Assert.Equal(5, query.Count);
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
        IResult suggestions = await controller.GetSuggestedCityNames("alpha", 5, CancellationToken.None);
        IResult list = await controller.List(includeArchived: true, CancellationToken.None);
        IResult listProvisioning = await controller.ListProvisioning(CancellationToken.None);
        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult getProvisioning = await controller.GetProvisioning(cityId, CancellationToken.None);
        IResult getWeather = await controller.GetWeather(cityId, CancellationToken.None);

        CityGenerationCatalogView catalogView = AssertResult<CityGenerationCatalogView>(generationCatalog, StatusCodes.Status200OK);
        Assert.Equal(["Mega City", "Neo Harbor"], catalogView.CityNamePresets);

        SimulationKindCatalogItemView[] kindsView = AssertResult<SimulationKindCatalogItemView[]>(simulationKinds, StatusCodes.Status200OK);
        Assert.Single(kindsView);
        Assert.True(kindsView[0].IsDefault);

        SuggestedCityNamesView suggestionsView = AssertResult<SuggestedCityNamesView>(suggestions, StatusCodes.Status200OK);
        Assert.Equal("alpha", suggestionsView.Seed);
        Assert.Equal(3, suggestionsView.Names.Length);

        CityListItemView[] listed = AssertResult<CityListItemView[]>(list, StatusCodes.Status200OK);
        CityListItemView[] provisioningListed = AssertResult<CityListItemView[]>(listProvisioning, StatusCodes.Status200OK);
        Assert.Single(listed);
        Assert.Single(provisioningListed);

        CityView cityView = AssertResult<CityView>(get, StatusCodes.Status200OK);
        Assert.Equal(cityId, cityView.CityId);
        Assert.Equal("Mega City", cityView.Name);

        CityProvisioningStatusView provisioningStatus = AssertResult<CityProvisioningStatusView>(getProvisioning, StatusCodes.Status200OK);
        Assert.Equal(city.PopulationBootstrapOperationId, provisioningStatus.PopulationBootstrapOperationId);

        CityWeatherView weatherView = AssertResult<CityWeatherView>(getWeather, StatusCodes.Status200OK);
        Assert.Equal("Clear", weatherView.CurrentType);
        Assert.Equal(18.5m, weatherView.TemperatureC);
    }

    [Fact]
    public async Task TopologyAndWeatherQueries_MapProjectedViewsAndMissingWeather()
    {
        Guid cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
        CityMapTopologyDto map = CreateMapTopologyDto(cityId);
        CityRoadGraphDto roadGraph = CreateRoadGraphDto(cityId);
        DistrictDto district = map.Districts[0];
        ResidentialBuildingDto building = map.ResidentialBuildings[0];
        CityAnchorDto anchor = map.Anchors[0];
        var sender = new FakeSender();
        sender.Handle<GetCityDistrictsQuery, IReadOnlyList<DistrictDto>>(_ => [district]);
        sender.Handle<GetCityResidentialBuildingsQuery, IReadOnlyList<ResidentialBuildingDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            Assert.Equal(district.DistrictId, query.DistrictId);
            return [building];
        });
        sender.Handle<GetCityAnchorsQuery, IReadOnlyList<CityAnchorDto>>(_ => [anchor]);
        sender.Handle<GetCityMapTopologyQuery, CityMapTopologyDto>(_ => map);
        sender.Handle<GetCityRoadGraphQuery, CityRoadGraphDto>(_ => roadGraph);
        sender.Handle<GetWeatherQuery, CityWeatherDto?>(_ => null);
        var controller = new CitiesController(sender);

        IResult districts = await controller.GetDistricts(cityId, CancellationToken.None);
        IResult buildings = await controller.GetResidentialBuildings(cityId, district.DistrictId, CancellationToken.None);
        IResult anchors = await controller.GetAnchors(cityId, CancellationToken.None);
        IResult getMap = await controller.GetMap(cityId, CancellationToken.None);
        IResult getRoadGraph = await controller.GetRoadGraph(cityId, CancellationToken.None);
        IResult missingWeather = await controller.GetWeather(cityId, CancellationToken.None);

        DistrictView[] districtViews = AssertResult<DistrictView[]>(districts, StatusCodes.Status200OK);
        ResidentialBuildingView[] buildingViews = AssertResult<ResidentialBuildingView[]>(buildings, StatusCodes.Status200OK);
        CityAnchorView[] anchorViews = AssertResult<CityAnchorView[]>(anchors, StatusCodes.Status200OK);
        CityMapTopologyView mapView = AssertResult<CityMapTopologyView>(getMap, StatusCodes.Status200OK);
        CityRoadGraphView graphView = AssertResult<CityRoadGraphView>(getRoadGraph, StatusCodes.Status200OK);

        Assert.Equal(district.Name, Assert.Single(districtViews).Name);
        Assert.Equal(building.Name, Assert.Single(buildingViews).Name);
        Assert.Equal(anchor.Name, Assert.Single(anchorViews).Name);
        Assert.Single(mapView.RoadNodes);
        Assert.Single(graphView.RoadSegments);
        AssertStatus(missingWeather, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task BootstrapAndLifecycleMutations_MapStatusesAndBodies()
    {
        Guid cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
        Guid populationOperationId = Guid.Parse("f91e16fd-ee76-4330-8dda-3fb5b8749d52");
        Guid economyOperationId = Guid.Parse("2c03f709-08b6-4d6c-8789-f45f5ecdd3a3");
        CityProvisioningModel provisioning = CreateProvisioningModel(cityId);
        var sender = new FakeSender();
        sender.Handle<RestartCityPopulationBootstrapCommand, RestartCityPopulationBootstrapResult>(_ =>
            RestartCityPopulationBootstrapResult.Restarted(populationOperationId, economyOperationId, "ClassicCity"));
        sender.Handle<RetryCityPopulationBootstrapProvisioningCommand, RetryCityPopulationBootstrapProvisioningResult>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(144, command.PlannedPeopleCountOverride);
            return RetryCityPopulationBootstrapProvisioningResult.Accepted(provisioning);
        });
        sender.Handle<CompleteCityPopulationBootstrapCommand, bool>(command =>
        {
            Assert.Equal(populationOperationId, command.OperationId);
            return true;
        });
        sender.Handle<FailCityPopulationBootstrapCommand, bool>(command =>
        {
            Assert.Equal("Population.SeedInvalid", command.FailureCode);
            return false;
        });
        sender.Handle<CompleteCityEconomyBootstrapCommand, bool>(_ => true);
        sender.Handle<FailCityEconomyBootstrapCommand, bool>(_ => true);
        sender.Handle<RenameCityCommand, bool>(command =>
        {
            Assert.Equal("Neo City", command.Name);
            return true;
        });
        sender.Handle<UpdateCityEnvironmentCommand, bool>(command =>
        {
            Assert.Equal("Arid", command.ClimateZone);
            Assert.Equal("Southern", command.Hemisphere);
            Assert.Equal(-120, command.UtcOffsetMinutes);
            return true;
        });
        sender.Handle<ArchiveCityCommand, bool>(_ => true);
        sender.Handle<DeleteCityCommand, DeleteCityResult>(_ => DeleteCityResult.NotAllowed);
        var controller = new CitiesController(sender);

        IResult retry = await controller.RetryPopulationBootstrap(cityId, CancellationToken.None);
        IResult retryProvisioning = await controller.RetryPopulationBootstrapProvisioning(
            cityId,
            new RetryCityPopulationBootstrapProvisioningRequest(144),
            CancellationToken.None);
        IResult completePopulation = await controller.CompletePopulationBootstrap(
            cityId,
            new CompleteCityPopulationBootstrapRequest(populationOperationId),
            CancellationToken.None);
        IResult failPopulation = await controller.FailPopulationBootstrap(
            cityId,
            new FailCityPopulationBootstrapRequest(populationOperationId, "Population.SeedInvalid"),
            CancellationToken.None);
        IResult completeEconomy = await controller.CompleteEconomyBootstrap(
            cityId,
            new CompleteCityEconomyBootstrapRequest(economyOperationId),
            CancellationToken.None);
        IResult failEconomy = await controller.FailEconomyBootstrap(
            cityId,
            new FailCityEconomyBootstrapRequest(economyOperationId, "Economy.UnitInvalid"),
            CancellationToken.None);
        IResult rename = await controller.Rename(cityId, new RenameCityRequest("Neo City"), CancellationToken.None);
        IResult updateEnvironment = await controller.UpdateEnvironment(
            cityId,
            new UpdateCityEnvironmentRequest("Arid", "Southern", -120),
            CancellationToken.None);
        IResult archive = await controller.Archive(cityId, CancellationToken.None);
        IResult delete = await controller.Delete(cityId, CancellationToken.None);

        CityPopulationBootstrapRestartedView retryView =
            AssertResult<CityPopulationBootstrapRestartedView>(retry, StatusCodes.Status200OK);
        Assert.Equal(populationOperationId, retryView.PopulationBootstrapOperationId);

        CityProvisioningView retryProvisioningView =
            AssertResult<CityProvisioningView>(retryProvisioning, StatusCodes.Status200OK);
        Assert.Equal(provisioning.CityId, retryProvisioningView.CityId);

        AssertStatus(completePopulation, StatusCodes.Status204NoContent);
        AssertStatus(failPopulation, StatusCodes.Status404NotFound);
        AssertStatus(completeEconomy, StatusCodes.Status204NoContent);
        AssertStatus(failEconomy, StatusCodes.Status204NoContent);
        AssertStatus(rename, StatusCodes.Status204NoContent);
        AssertStatus(updateEnvironment, StatusCodes.Status204NoContent);
        AssertStatus(archive, StatusCodes.Status204NoContent);
        Assert.Equal(
            "SimulationCore.City.DeleteNotAllowed",
            GetAnonymousProperty<string>(delete, "code", StatusCodes.Status409Conflict));
    }
}
