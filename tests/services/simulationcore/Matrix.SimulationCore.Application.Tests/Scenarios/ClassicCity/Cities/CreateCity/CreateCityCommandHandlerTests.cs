using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Simulation;
using System.Data;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CreateCity;

public sealed class CreateCityCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProvisioningCorrelationAlreadyExists_ReturnsExistingCityWithoutCreatingNewOne()
    {
        Guid provisioningCorrelationId = Guid.NewGuid();
        var existingCity = ClassicCityTestSupport.CreateCity(
            name: "Existing City",
            provisioningCorrelationId: provisioningCorrelationId);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityByProvisioningCorrelationId = existingCity
        };
        var districtRepository = new TopologyTestSupport.FakeDistrictRepository();
        var residentialBuildingRepository = new TopologyTestSupport.FakeResidentialBuildingRepository();
        var cityAnchorRepository = new TopologyTestSupport.FakeCityAnchorRepository();
        var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository();
        var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository();
        var cityWeatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
        var strategy = new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
        {
            Descriptor = new SimulationKindDescriptor(
                Kind: SimulationKind.ClassicCity,
                DisplayName: "Classic City",
                Description: "Classic city simulation.",
                SupportsAutomaticPopulationBootstrap: true)
        };
        var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new CreateCityCommandHandler(
            cityRepository,
            districtRepository,
            residentialBuildingRepository,
            cityAnchorRepository,
            roadNodeRepository,
            roadSegmentRepository,
            cityWeatherRepository,
            clockRepository,
            [strategy],
            outboxWriter,
            unitOfWork);

        var result = await handler.Handle(CreateCommand(provisioningCorrelationId), CancellationToken.None);

        Assert.Equal(existingCity.Id.Value, result.CityId);
        Assert.Equal(existingCity.PopulationBootstrapOperationId, result.PopulationBootstrapOperationId);
        Assert.Equal(existingCity.EconomyBootstrapOperationId, result.EconomyBootstrapOperationId);
        Assert.Equal(existingCity.SimulationKind.ToString(), result.SimulationKind);
        Assert.Equal(provisioningCorrelationId, cityRepository.RequestedProvisioningCorrelationId);
        Assert.Null(cityRepository.AddedCity);
        Assert.Empty(districtRepository.AddedDistricts);
        Assert.Empty(residentialBuildingRepository.AddedBuildings);
        Assert.Empty(cityAnchorRepository.AddedAnchors);
        Assert.Empty(roadNodeRepository.AddedRoadNodes);
        Assert.Empty(roadSegmentRepository.AddedRoadSegments);
        Assert.Null(cityWeatherRepository.AddedWeather);
        Assert.Null(clockRepository.AddedClock);
        Assert.Null(strategy.RequestedCommand);
        Assert.Empty(outboxWriter.CityEvents);
        Assert.Empty(outboxWriter.WeatherEvents);
        Assert.Equal(0, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenCityIsNew_PersistsBootstrapPlanPublishesEventsAndReturnsDto()
    {
        Guid provisioningCorrelationId = Guid.NewGuid();
        var city = ClassicCityTestSupport.CreateCity(
            name: "Neo Tokyo",
            provisioningCorrelationId: provisioningCorrelationId);
        var district = TopologyTestSupport.CreateDistrict(city.Id, "Downtown");
        var northNode = TopologyTestSupport.CreateRoadNode(city.Id, district.Id, "North Junction");
        var southNode = TopologyTestSupport.CreateRoadNode(city.Id, district.Id, "South Junction");
        var residentialBuilding = TopologyTestSupport.CreateResidentialBuilding(
            cityId: city.Id,
            districtId: district.Id,
            name: "River Tower",
            accessRoadNodeId: northNode.Id);
        var cityAnchor = TopologyTestSupport.CreateCityAnchor(
            cityId: city.Id,
            districtId: district.Id,
            name: "Central Hospital",
            accessRoadNodeId: southNode.Id);
        var roadSegment = TopologyTestSupport.CreateRoadSegment(
            cityId: city.Id,
            districtId: district.Id,
            fromRoadNodeId: northNode.Id,
            toRoadNodeId: southNode.Id,
            name: "Downtown Connector");
        var topology = new CityTopologySeed(
            Districts: [district],
            ResidentialBuildings: [residentialBuilding],
            Anchors: [cityAnchor],
            RoadNodes: [northNode, southNode],
            RoadSegments: [roadSegment]);
        var weather = WeatherTestSupport.CreateCityWeather(city.Id);
        var clock = SimulationTestSupport.CreateClock(city.Id.Value);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
        var districtRepository = new TopologyTestSupport.FakeDistrictRepository();
        var residentialBuildingRepository = new TopologyTestSupport.FakeResidentialBuildingRepository();
        var cityAnchorRepository = new TopologyTestSupport.FakeCityAnchorRepository();
        var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository();
        var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository();
        var cityWeatherRepository = new WeatherTestSupport.FakeCityWeatherRepository();
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
        var strategy = new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
        {
            Descriptor = new SimulationKindDescriptor(
                Kind: SimulationKind.ClassicCity,
                DisplayName: "Classic City",
                Description: "Classic city simulation.",
                SupportsAutomaticPopulationBootstrap: true),
            Plan = new CitySimulationBootstrapPlan(
                City: city,
                Clock: clock,
                Topology: topology,
                Weather: weather,
                SupportsAutomaticPopulationBootstrap: true)
        };
        var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
        var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
        var handler = new CreateCityCommandHandler(
            cityRepository,
            districtRepository,
            residentialBuildingRepository,
            cityAnchorRepository,
            roadNodeRepository,
            roadSegmentRepository,
            cityWeatherRepository,
            clockRepository,
            [strategy],
            outboxWriter,
            unitOfWork);
        var command = CreateCommand(provisioningCorrelationId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Same(command, strategy.RequestedCommand);
        Assert.Equal(provisioningCorrelationId, cityRepository.RequestedProvisioningCorrelationId);
        Assert.Same(city, cityRepository.AddedCity);
        Assert.Same(weather, cityWeatherRepository.AddedWeather);
        Assert.Same(clock, clockRepository.AddedClock);
        Assert.Equal([district], districtRepository.AddedDistricts);
        Assert.Equal([residentialBuilding], residentialBuildingRepository.AddedBuildings);
        Assert.Equal([cityAnchor], cityAnchorRepository.AddedAnchors);
        Assert.Equal([northNode, southNode], roadNodeRepository.AddedRoadNodes);
        Assert.Equal([roadSegment], roadSegmentRepository.AddedRoadSegments);
        Assert.Equal(1, unitOfWork.ExecuteInTransactionCallCount);
        Assert.Equal(IsolationLevel.ReadCommitted, unitOfWork.LastIsolationLevel);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        var cityEvent = Assert.Single(outboxWriter.CityEvents);
        var createdEvent = Assert.IsType<CityCreatedDomainEvent>(cityEvent);
        Assert.Equal(city.Id, createdEvent.CityId);
        var weatherEvent = Assert.Single(outboxWriter.WeatherEvents);
        var weatherCreatedEvent = Assert.IsType<CityWeatherCreatedDomainEvent>(weatherEvent);
        Assert.Equal(city.Id, weatherCreatedEvent.CityId);
        Assert.Equal(city.Id.Value, result.CityId);
        Assert.Equal(city.PopulationBootstrapOperationId, result.PopulationBootstrapOperationId);
        Assert.Equal(city.EconomyBootstrapOperationId, result.EconomyBootstrapOperationId);
        Assert.Equal("ClassicCity", result.SimulationKind);
    }

    private static CreateCityCommand CreateCommand(Guid provisioningCorrelationId)
    {
        return new CreateCityCommand(
            Name: "Neo Tokyo",
            SimulationKind: "ClassicCity",
            ClimateZone: "Temperate",
            Hemisphere: "Northern",
            UtcOffsetMinutes: 180,
            GenerationSeed: "neo-tokyo-seed",
            SizeTier: "Medium",
            UrbanDensity: "Balanced",
            DevelopmentLevel: "Balanced",
            EconomyProfile: "Balanced",
            PopulationOccupancyProfile: "Balanced",
            InitialWeatherMode: "Manual",
            InitialWeatherType: "Clear",
            InitialWeatherSeverity: "Calm",
            InitialWeatherTemperatureC: 18m,
            StartSimTimeUtc: DateTimeOffset.Parse("2048-08-01T09:00:00+00:00"),
            SpeedMultiplier: 60m,
            PlannedPeopleCount: 25_000,
            ProvisioningCorrelationId: provisioningCorrelationId,
            ScenarioModelSetVersion: "classic-city-v3");
    }
}
