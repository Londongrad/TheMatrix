using System.Data;
using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Application.Tests.TestSupport;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CreateCity
{
    public sealed class CreateCityCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenProvisioningCorrelationAlreadyExists_ReturnsExistingCityWithoutCreatingNewOne()
        {
            var provisioningCorrelationId = Guid.NewGuid();
            City existingCity = ClassicCityTestSupport.CreateCity(
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
                SupportsAutomaticPopulationBootstrap = true
            };
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var simulationInstanceRepository = new SimulationTestSupport.FakeSimulationInstanceRepository();
            var handler = new CreateCityCommandHandler(
                simulationInstanceRepository: simulationInstanceRepository,
                cityRepository: cityRepository,
                districtRepository: districtRepository,
                residentialBuildingRepository: residentialBuildingRepository,
                cityAnchorRepository: cityAnchorRepository,
                roadNodeRepository: roadNodeRepository,
                roadSegmentRepository: roadSegmentRepository,
                cityWeatherRepository: cityWeatherRepository,
                clockRepository: clockRepository,
                simulationBootstrapStrategy: strategy,
                outboxWriter: outboxWriter,
                unitOfWork: unitOfWork);

            CityCreatedDto result = await handler.Handle(
                request: CreateCommand(provisioningCorrelationId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: existingCity.Id.Value,
                actual: result.CityId);
            Assert.Equal(
                expected: existingCity.PopulationBootstrapOperationId,
                actual: result.PopulationBootstrapOperationId);
            Assert.Equal(
                expected: existingCity.EconomyBootstrapOperationId,
                actual: result.EconomyBootstrapOperationId);
            Assert.Equal(
                expected: SimulationKind.ClassicCity.ToString(),
                actual: result.SimulationKind);
            Assert.Equal(
                expected: provisioningCorrelationId,
                actual: cityRepository.RequestedProvisioningCorrelationId);
            Assert.Null(cityRepository.AddedCity);
            Assert.Null(simulationInstanceRepository.AddedInstance);
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
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteInTransactionCallCount);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }

        [Fact]
        public async Task Handle_WhenCityIsNew_PersistsBootstrapPlanPublishesEventsAndReturnsDto()
        {
            var provisioningCorrelationId = Guid.NewGuid();
            City city = ClassicCityTestSupport.CreateCity(
                name: "Neo Tokyo",
                provisioningCorrelationId: provisioningCorrelationId);
            District district = TopologyTestSupport.CreateDistrict(
                cityId: city.Id,
                name: "Downtown");
            RoadNode northNode = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "North Junction");
            RoadNode southNode = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "South Junction");
            ResidentialBuilding residentialBuilding = TopologyTestSupport.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: district.Id,
                name: "River Tower",
                accessRoadNodeId: northNode.Id);
            CityAnchor cityAnchor = TopologyTestSupport.CreateCityAnchor(
                cityId: city.Id,
                districtId: district.Id,
                name: "Central Hospital",
                accessRoadNodeId: southNode.Id);
            RoadSegment roadSegment = TopologyTestSupport.CreateRoadSegment(
                cityId: city.Id,
                districtId: district.Id,
                fromRoadNodeId: northNode.Id,
                toRoadNodeId: southNode.Id,
                name: "Downtown Connector");
            var topology = new CityTopologySeed(
                Districts: [district],
                ResidentialBuildings: [residentialBuilding],
                Anchors: [cityAnchor],
                RoadNodes:
                [
                    northNode,
                    southNode
                ],
                RoadSegments: [roadSegment]);
            CityWeather weather = WeatherTestSupport.CreateCityWeather(city.Id);
            SimulationClock clock = SimulationTestSupport.CreateClock(city.Id.Value);
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
                SupportsAutomaticPopulationBootstrap = true,
                Plan = new CitySimulationBootstrapPlan(
                    Instance: SimulationTestSupport.CreateInstance(city),
                    City: city,
                    Clock: clock,
                    Topology: topology,
                    Weather: weather,
                    SupportsAutomaticPopulationBootstrap: true)
            };
            var outboxWriter = new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter();
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork();
            var simulationInstanceRepository = new SimulationTestSupport.FakeSimulationInstanceRepository();
            var handler = new CreateCityCommandHandler(
                simulationInstanceRepository: simulationInstanceRepository,
                cityRepository: cityRepository,
                districtRepository: districtRepository,
                residentialBuildingRepository: residentialBuildingRepository,
                cityAnchorRepository: cityAnchorRepository,
                roadNodeRepository: roadNodeRepository,
                roadSegmentRepository: roadSegmentRepository,
                cityWeatherRepository: cityWeatherRepository,
                clockRepository: clockRepository,
                simulationBootstrapStrategy: strategy,
                outboxWriter: outboxWriter,
                unitOfWork: unitOfWork);
            CreateCityCommand command = CreateCommand(provisioningCorrelationId);

            CityCreatedDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            Assert.Same(
                expected: command,
                actual: strategy.RequestedCommand);
            Assert.Equal(
                expected: provisioningCorrelationId,
                actual: cityRepository.RequestedProvisioningCorrelationId);
            Assert.Same(
                expected: city,
                actual: cityRepository.AddedCity);
            Assert.Same(
                expected: strategy.Plan.Instance,
                actual: simulationInstanceRepository.AddedInstance);
            Assert.Same(
                expected: weather,
                actual: cityWeatherRepository.AddedWeather);
            Assert.Same(
                expected: clock,
                actual: clockRepository.AddedClock);
            Assert.Equal(
                expected: [district],
                actual: districtRepository.AddedDistricts);
            Assert.Equal(
                expected: [residentialBuilding],
                actual: residentialBuildingRepository.AddedBuildings);
            Assert.Equal(
                expected: [cityAnchor],
                actual: cityAnchorRepository.AddedAnchors);
            Assert.Equal(
                expected:
                [
                    northNode,
                    southNode
                ],
                actual: roadNodeRepository.AddedRoadNodes);
            Assert.Equal(
                expected: [roadSegment],
                actual: roadSegmentRepository.AddedRoadSegments);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteInTransactionCallCount);
            Assert.Equal(
                expected: IsolationLevel.ReadCommitted,
                actual: unitOfWork.LastIsolationLevel);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            IDomainEvent simulationEvent = Assert.Single(outboxWriter.SimulationEvents);
            SimulationCreatedDomainEvent simulationCreatedEvent =
                Assert.IsType<SimulationCreatedDomainEvent>(simulationEvent);
            Assert.Equal(
                expected: strategy.Plan.Instance.Id,
                actual: simulationCreatedEvent.SimulationId);
            IDomainEvent cityEvent = Assert.Single(outboxWriter.CityEvents);
            CityCreatedDomainEvent createdEvent = Assert.IsType<CityCreatedDomainEvent>(cityEvent);
            Assert.Equal(
                expected: city.Id,
                actual: createdEvent.CityId);
            IDomainEvent weatherEvent = Assert.Single(outboxWriter.WeatherEvents);
            CityWeatherCreatedDomainEvent weatherCreatedEvent =
                Assert.IsType<CityWeatherCreatedDomainEvent>(weatherEvent);
            Assert.Equal(
                expected: city.Id,
                actual: weatherCreatedEvent.CityId);
            Assert.Equal(
                expected: city.Id.Value,
                actual: result.CityId);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: result.PopulationBootstrapOperationId);
            Assert.Equal(
                expected: city.EconomyBootstrapOperationId,
                actual: result.EconomyBootstrapOperationId);
            Assert.Equal(
                expected: "ClassicCity",
                actual: result.SimulationKind);
        }

        [Fact]
        public async Task Handle_WhenTransactionFailsButProvisionedCityAppears_ReturnsExistingCity()
        {
            var provisioningCorrelationId = Guid.NewGuid();
            City createdCity = ClassicCityTestSupport.CreateCity(
                name: "Neo Tokyo",
                provisioningCorrelationId: provisioningCorrelationId);
            City existingCity = ClassicCityTestSupport.CreateCity(
                name: "Recovered City",
                provisioningCorrelationId: provisioningCorrelationId);
            var topology = new CityTopologySeed(
                Districts: [],
                ResidentialBuildings: [],
                Anchors: [],
                RoadNodes: [],
                RoadSegments: []);
            SimulationClock clock = SimulationTestSupport.CreateClock(createdCity.Id.Value);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            cityRepository.CityByProvisioningCorrelationSequence.Enqueue(null);
            cityRepository.CityByProvisioningCorrelationSequence.Enqueue(existingCity);
            var strategy = new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
            {
                SupportsAutomaticPopulationBootstrap = true,
                Plan = new CitySimulationBootstrapPlan(
                    Instance: SimulationTestSupport.CreateInstance(createdCity),
                    City: createdCity,
                    Clock: clock,
                    Topology: topology,
                    Weather: null,
                    SupportsAutomaticPopulationBootstrap: true)
            };
            var unitOfWork = new ApplicationTestSupport.FakeUnitOfWork
            {
                ExceptionToThrowAfterAction = new InvalidOperationException("duplicate provisioning race")
            };
            var handler = new CreateCityCommandHandler(
                simulationInstanceRepository: new SimulationTestSupport.FakeSimulationInstanceRepository(),
                cityRepository: cityRepository,
                districtRepository: new TopologyTestSupport.FakeDistrictRepository(),
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository(),
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository(),
                roadNodeRepository: new TopologyTestSupport.FakeRoadNodeRepository(),
                roadSegmentRepository: new TopologyTestSupport.FakeRoadSegmentRepository(),
                cityWeatherRepository: new WeatherTestSupport.FakeCityWeatherRepository(),
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository(),
                simulationBootstrapStrategy: strategy,
                outboxWriter: new ClassicCityTestSupport.FakeSimulationCoreOutboxWriter(),
                unitOfWork: unitOfWork);

            CityCreatedDto result = await handler.Handle(
                request: CreateCommand(provisioningCorrelationId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: existingCity.Id.Value,
                actual: result.CityId);
            Assert.Equal(
                expected: existingCity.PopulationBootstrapOperationId,
                actual: result.PopulationBootstrapOperationId);
            Assert.Equal(
                expected: existingCity.EconomyBootstrapOperationId,
                actual: result.EconomyBootstrapOperationId);
            Assert.Equal(
                expected: SimulationKind.ClassicCity.ToString(),
                actual: result.SimulationKind);
            Assert.Equal(
                expected: 2,
                actual: cityRepository.GetByProvisioningCorrelationCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteInTransactionCallCount);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
        }

        private static CreateCityCommand CreateCommand(Guid provisioningCorrelationId)
        {
            return new CreateCityCommand(
                Name: "Neo Tokyo",
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
}
