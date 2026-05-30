using System.Net;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Provisioning
{
    public sealed class ClassicCityProvisioningOrchestratorExecutionTests
    {
        [Fact]
        public async Task ProvisionAsync_WhenCityDoesNotExist_ThrowsInvalidOperationException()
        {
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: new ProvisioningTestSupport.FakeMediator(),
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                economyClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationClient: new ProvisioningTestSupport.FakeCityPopulationBootstrapClient(),
                supportsAutomaticPopulationBootstrap: true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                orchestrator.ProvisionAsync(
                    cityId: Guid.NewGuid(),
                    simulationKind: "ClassicCity",
                    populationBootstrapOperationId: Guid.NewGuid(),
                    economyBootstrapOperationId: Guid.NewGuid(),
                    plannedPeopleCountOverride: 25_000,
                    heartbeatAsync: null,
                    cancellationToken: CancellationToken.None));
        }

        [Fact]
        public async Task
            ProvisionAsync_WhenEconomyCompletesAndPopulationBootstrapIsUnsupported_CompletesEconomyAndSkipsPopulation()
        {
            int heartbeatCallCount = 0;
            City city = ClassicCityTestSupport.CreateCity(
                name: "Manual Population City",
                requiresEconomyBootstrap: true);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is CompleteCityEconomyBootstrapCommand
                    ? true
                    : null
            };
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient
            {
                Result = new CityEconomyBootstrapResult(
                    UnitKind: "Currency",
                    UnitCode: "NCR",
                    UnitDisplayName: "Neo Credits",
                    UnitSymbol: "N$")
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: cityRepository,
                economyClient: economyClient,
                populationClient: populationClient,
                supportsAutomaticPopulationBootstrap: false);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 12_345,
                heartbeatAsync: _ =>
                {
                    heartbeatCallCount++;
                    return Task.CompletedTask;
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: heartbeatCallCount);
            Assert.Equal(
                expected: city.Id.Value,
                actual: economyClient.RequestedCityId);
            Assert.Equal(
                expected: "ClassicCity",
                actual: economyClient.RequestedSimulationKind);
            Assert.Equal(
                expected: city.GenerationProfile.EconomyProfile.ToString(),
                actual: economyClient.RequestedEconomyProfile);
            object sentCommand = Assert.Single(mediator.SentRequests);
            CompleteCityEconomyBootstrapCommand completeCommand =
                Assert.IsType<CompleteCityEconomyBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: city.Id.Value,
                actual: completeCommand.CityId);
            Assert.Equal(
                expected: city.EconomyBootstrapOperationId,
                actual: completeCommand.OperationId);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Completed",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: "NCR",
                actual: result.EconomyBootstrap.UnitCode);
            Assert.Equal(
                expected: "Skipped",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: 12_345,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
        }

        [Fact]
        public async Task ProvisionAsync_WhenEconomyBootstrapFails_ReturnsFailureAndDoesNotRunPopulationBootstrap()
        {
            City city = ClassicCityTestSupport.CreateCity(
                name: "Broken Economy City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityEconomyBootstrapCommand
                    ? true
                    : null
            };
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient
            {
                ExceptionToThrow = new HttpRequestException(
                    message: "conflict",
                    inner: null,
                    statusCode: HttpStatusCode.Conflict)
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: cityRepository,
                economyClient: economyClient,
                populationClient: populationClient,
                supportsAutomaticPopulationBootstrap: true);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 25_000,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityEconomyBootstrapCommand failCommand = Assert.IsType<FailCityEconomyBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: city.Id.Value,
                actual: failCommand.CityId);
            Assert.Equal(
                expected: city.EconomyBootstrapOperationId,
                actual: failCommand.OperationId);
            Assert.Equal(
                expected: EconomyBootstrapFailureCodes.EconomyConflict,
                actual: failCommand.FailureCode);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Failed",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: EconomyBootstrapFailureCodes.EconomyConflict,
                actual: result.EconomyBootstrap.FailureCode);
            Assert.Equal(
                expected: "Pending",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: 25_000,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
        }

        [Fact]
        public async Task
            ProvisionAsync_WhenCityIsAlreadyActive_ReturnsCompletedPopulationWithoutCallingPopulationBootstrapClient()
        {
            City city = ClassicCityTestSupport.CreateCity("Active City");
            var mediator = new ProvisioningTestSupport.FakeMediator();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient();
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: cityRepository,
                economyClient: economyClient,
                populationClient: populationClient,
                supportsAutomaticPopulationBootstrap: true);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: null,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            Assert.Empty(mediator.SentRequests);
            Assert.Null(economyClient.RequestedCityId);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Completed",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: "Completed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: city.GenerationProfile.PlannedPeopleCount,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
        }

        [Fact]
        public async Task
            ProvisionAsync_WhenPopulationBootstrapAlreadyFailed_ReturnsFailedPopulationStateWithoutCallingPopulationClient()
        {
            var failedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                name: "Population Failure City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            Assert.True(
                city.TryCompleteEconomyBootstrap(
                    operationId: city.EconomyBootstrapOperationId,
                    completedAtUtc: failedAtUtc.AddMinutes(-5)));
            Assert.True(
                city.TryFailPopulationBootstrap(
                    operationId: city.PopulationBootstrapOperationId,
                    failureCode: "population_conflict",
                    failedAtUtc: failedAtUtc));
            var mediator = new ProvisioningTestSupport.FakeMediator();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient();
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: cityRepository,
                economyClient: economyClient,
                populationClient: populationClient,
                supportsAutomaticPopulationBootstrap: true);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 20_000,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            Assert.Empty(mediator.SentRequests);
            Assert.Null(economyClient.RequestedCityId);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Completed",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: "Failed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: "POPULATION_CONFLICT",
                actual: result.PopulationBootstrap.FailureCode);
            Assert.Equal(
                expected: 20_000,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
        }

        [Fact]
        public async Task
            ProvisionAsync_WhenResidentialCapacityIsMissing_FailsPopulationBootstrapWithoutCallingPopulationClient()
        {
            var completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                name: "No Capacity City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            Assert.True(
                city.TryCompleteEconomyBootstrap(
                    operationId: city.EconomyBootstrapOperationId,
                    completedAtUtc: completedAtUtc));
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityPopulationBootstrapCommand
                    ? true
                    : null
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            var orchestrator = new ClassicCityProvisioningOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository(),
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository(),
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository
                {
                    ClockBySimulationId = SimulationTestSupport.CreateClock(city.Id.Value)
                },
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        Descriptor = new SimulationKindDescriptor(
                            Kind: SimulationKind.ClassicCity,
                            DisplayName: "Classic City",
                            Description: "Classic city simulation.",
                            SupportsAutomaticPopulationBootstrap: true)
                    },
                economyBootstrapClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationBootstrapClient: populationClient,
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 12_345,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityPopulationBootstrapCommand failCommand =
                Assert.IsType<FailCityPopulationBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: city.Id.Value,
                actual: failCommand.CityId);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: failCommand.OperationId);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationResidentialCapacityMissing,
                actual: failCommand.FailureCode);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Completed",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: "Failed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: 12_345,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
            Assert.Equal(
                expected: 0,
                actual: result.PopulationBootstrap.ResidentialCapacity);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationResidentialCapacityMissing,
                actual: result.PopulationBootstrap.FailureCode);
        }

        [Fact]
        public async Task ProvisionAsync_WhenSimulationClockIsMissing_FailsPopulationBootstrapWithInvalidResponseCode()
        {
            var completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                name: "Clockless City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            Assert.True(
                city.TryCompleteEconomyBootstrap(
                    operationId: city.EconomyBootstrapOperationId,
                    completedAtUtc: completedAtUtc));
            District district = TopologyTestSupport.CreateDistrict(
                cityId: city.Id,
                name: "Downtown");
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: district.Id,
                name: "River Tower");
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityPopulationBootstrapCommand
                    ? true
                    : null
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            var orchestrator = new ClassicCityProvisioningOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository(),
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository
                {
                    Buildings = [building]
                },
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository(),
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        Descriptor = new SimulationKindDescriptor(
                            Kind: SimulationKind.ClassicCity,
                            DisplayName: "Classic City",
                            Description: "Classic city simulation.",
                            SupportsAutomaticPopulationBootstrap: true)
                    },
                economyBootstrapClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationBootstrapClient: populationClient,
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: null,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityPopulationBootstrapCommand failCommand =
                Assert.IsType<FailCityPopulationBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationResponseInvalid,
                actual: failCommand.FailureCode);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Failed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationResponseInvalid,
                actual: result.PopulationBootstrap.FailureCode);
        }

        [Fact]
        public async Task ProvisionAsync_WhenPopulationSummaryIsInconsistent_FailsPopulationBootstrapWithSummaryCode()
        {
            var completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                name: "Inconsistent Summary City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            Assert.True(
                city.TryCompleteEconomyBootstrap(
                    operationId: city.EconomyBootstrapOperationId,
                    completedAtUtc: completedAtUtc));
            District district = TopologyTestSupport.CreateDistrict(
                cityId: city.Id,
                name: "Downtown");
            RoadNode node = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "North Junction");
            CityAnchor anchor = TopologyTestSupport.CreateCityAnchor(
                cityId: city.Id,
                districtId: district.Id,
                name: "Central Hospital",
                accessRoadNodeId: node.Id);
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: district.Id,
                name: "River Tower",
                accessRoadNodeId: node.Id);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityPopulationBootstrapCommand
                    ? true
                    : null
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient
            {
                Result = new CityPopulationBootstrapSummary(
                    CityId: city.Id.Value,
                    RequestedPeopleCount: 240,
                    GeneratedPeopleCount: 241,
                    HouseholdCount: 100,
                    HousedHouseholdCount: 90,
                    HomelessHouseholdCount: 10,
                    HousedPeopleCount: 220,
                    HomelessPeopleCount: 21)
            };
            var orchestrator = new ClassicCityProvisioningOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository
                {
                    Anchors = [anchor]
                },
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository
                {
                    Buildings = [building]
                },
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository
                {
                    ClockBySimulationId = SimulationTestSupport.CreateClock(city.Id.Value)
                },
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        Descriptor = new SimulationKindDescriptor(
                            Kind: SimulationKind.ClassicCity,
                            DisplayName: "Classic City",
                            Description: "Classic city simulation.",
                            SupportsAutomaticPopulationBootstrap: true)
                    },
                economyBootstrapClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationBootstrapClient: populationClient,
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 240,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityPopulationBootstrapCommand failCommand =
                Assert.IsType<FailCityPopulationBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationSummaryInconsistent,
                actual: failCommand.FailureCode);
            Assert.NotNull(populationClient.RequestedRequest);
            Assert.Equal(
                expected: city.Id.Value,
                actual: populationClient.RequestedRequest!.CityId);
            Assert.Equal(
                expected: 240,
                actual: populationClient.RequestedRequest.PeopleCount);
            Assert.Single(populationClient.RequestedRequest.CityAnchors);
            Assert.Single(populationClient.RequestedRequest.ResidentialBuildings);
            Assert.Equal(
                expected: "Failed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: 240,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
            Assert.Equal(
                expected: building.ResidentCapacity.Value,
                actual: result.PopulationBootstrap.ResidentialCapacity);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationSummaryInconsistent,
                actual: result.PopulationBootstrap.FailureCode);
        }

        [Fact]
        public async Task ProvisionAsync_WhenPopulationBootstrapSucceeds_CompletesPopulationAndMapsSummary()
        {
            int heartbeatCallCount = 0;
            var completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                name: "Successful Population City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            Assert.True(
                city.TryCompleteEconomyBootstrap(
                    operationId: city.EconomyBootstrapOperationId,
                    completedAtUtc: completedAtUtc));
            District district = TopologyTestSupport.CreateDistrict(
                cityId: city.Id,
                name: "Downtown");
            RoadNode node = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "North Junction");
            CityAnchor anchor = TopologyTestSupport.CreateCityAnchor(
                cityId: city.Id,
                districtId: district.Id,
                name: "Central Hospital",
                accessRoadNodeId: node.Id);
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: district.Id,
                name: "River Tower",
                accessRoadNodeId: node.Id);
            SimulationClock clock = SimulationTestSupport.CreateClock(city.Id.Value);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is CompleteCityPopulationBootstrapCommand
                    ? true
                    : null
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient
            {
                Result = new CityPopulationBootstrapSummary(
                    CityId: city.Id.Value,
                    RequestedPeopleCount: 200,
                    GeneratedPeopleCount: 200,
                    HouseholdCount: 70,
                    HousedHouseholdCount: 70,
                    HomelessHouseholdCount: 0,
                    HousedPeopleCount: 200,
                    HomelessPeopleCount: 0)
            };
            var orchestrator = new ClassicCityProvisioningOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository
                {
                    Anchors = [anchor]
                },
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository
                {
                    Buildings = [building]
                },
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository
                {
                    ClockBySimulationId = clock
                },
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        Descriptor = new SimulationKindDescriptor(
                            Kind: SimulationKind.ClassicCity,
                            DisplayName: "Classic City",
                            Description: "Classic city simulation.",
                            SupportsAutomaticPopulationBootstrap: true)
                    },
                economyBootstrapClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationBootstrapClient: populationClient,
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 200,
                heartbeatAsync: _ =>
                {
                    heartbeatCallCount++;
                    return Task.CompletedTask;
                },
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            CompleteCityPopulationBootstrapCommand completeCommand =
                Assert.IsType<CompleteCityPopulationBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: city.Id.Value,
                actual: completeCommand.CityId);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: completeCommand.OperationId);
            Assert.Equal(
                expected: 2,
                actual: heartbeatCallCount);
            Assert.NotNull(populationClient.RequestedRequest);
            Assert.Equal(
                expected: city.Id.Value,
                actual: populationClient.RequestedRequest!.CityId);
            Assert.Equal(
                expected: DateOnly.FromDateTime(clock.CurrentTime.ValueUtc.UtcDateTime),
                actual: populationClient.RequestedRequest.CurrentDate);
            Assert.Equal(
                expected: clock.CurrentTime.ValueUtc,
                actual: populationClient.RequestedRequest.CreatedAtUtc);
            Assert.Equal(
                expected: 200,
                actual: populationClient.RequestedRequest.PeopleCount);
            Assert.Single(populationClient.RequestedRequest.CityAnchors);
            Assert.Single(populationClient.RequestedRequest.ResidentialBuildings);
            Assert.Equal(
                expected: "Completed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: 200,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
            Assert.Equal(
                expected: building.ResidentCapacity.Value,
                actual: result.PopulationBootstrap.ResidentialCapacity);
            Assert.NotNull(result.PopulationBootstrap.Summary);
            Assert.Equal(
                expected: 200,
                actual: result.PopulationBootstrap.Summary!.GeneratedPeopleCount);
            Assert.Equal(
                expected: "Completed",
                actual: result.EconomyBootstrap.Status);
        }

        [Fact]
        public async Task ProvisionAsync_WhenPopulationBootstrapTimesOut_ReturnsPopulationTimeout()
        {
            var completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                name: "Timeout Population City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            Assert.True(
                city.TryCompleteEconomyBootstrap(
                    operationId: city.EconomyBootstrapOperationId,
                    completedAtUtc: completedAtUtc));
            District district = TopologyTestSupport.CreateDistrict(
                cityId: city.Id,
                name: "Downtown");
            RoadNode node = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "North Junction");
            CityAnchor anchor = TopologyTestSupport.CreateCityAnchor(
                cityId: city.Id,
                districtId: district.Id,
                name: "Central Hospital",
                accessRoadNodeId: node.Id);
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: district.Id,
                name: "River Tower",
                accessRoadNodeId: node.Id);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityPopulationBootstrapCommand
                    ? true
                    : null
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient
            {
                ExceptionToThrow = new OperationCanceledException("timed out")
            };
            var orchestrator = new ClassicCityProvisioningOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository
                {
                    Anchors = [anchor]
                },
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository
                {
                    Buildings = [building]
                },
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository
                {
                    ClockBySimulationId = SimulationTestSupport.CreateClock(city.Id.Value)
                },
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        Descriptor = new SimulationKindDescriptor(
                            Kind: SimulationKind.ClassicCity,
                            DisplayName: "Classic City",
                            Description: "Classic city simulation.",
                            SupportsAutomaticPopulationBootstrap: true)
                    },
                economyBootstrapClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationBootstrapClient: populationClient,
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 200,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityPopulationBootstrapCommand failCommand =
                Assert.IsType<FailCityPopulationBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationTimeout,
                actual: failCommand.FailureCode);
            Assert.Equal(
                expected: "Failed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationTimeout,
                actual: result.PopulationBootstrap.FailureCode);
        }

        [Fact]
        public async Task ProvisionAsync_WhenPopulationBootstrapReturnsValidationError_MapsValidationFailureCode()
        {
            var completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
            City city = ClassicCityTestSupport.CreateCity(
                name: "Validation Population City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            Assert.True(
                city.TryCompleteEconomyBootstrap(
                    operationId: city.EconomyBootstrapOperationId,
                    completedAtUtc: completedAtUtc));
            District district = TopologyTestSupport.CreateDistrict(
                cityId: city.Id,
                name: "Downtown");
            RoadNode node = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "North Junction");
            CityAnchor anchor = TopologyTestSupport.CreateCityAnchor(
                cityId: city.Id,
                districtId: district.Id,
                name: "Central Hospital",
                accessRoadNodeId: node.Id);
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: city.Id,
                districtId: district.Id,
                name: "River Tower",
                accessRoadNodeId: node.Id);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityPopulationBootstrapCommand
                    ? true
                    : null
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient
            {
                ExceptionToThrow = new HttpRequestException(
                    message: "validation failed",
                    inner: null,
                    statusCode: HttpStatusCode.UnprocessableEntity)
            };
            var orchestrator = new ClassicCityProvisioningOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository
                {
                    Anchors = [anchor]
                },
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository
                {
                    Buildings = [building]
                },
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository
                {
                    ClockBySimulationId = SimulationTestSupport.CreateClock(city.Id.Value)
                },
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        Descriptor = new SimulationKindDescriptor(
                            Kind: SimulationKind.ClassicCity,
                            DisplayName: "Classic City",
                            Description: "Classic city simulation.",
                            SupportsAutomaticPopulationBootstrap: true)
                    },
                economyBootstrapClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationBootstrapClient: populationClient,
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 200,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityPopulationBootstrapCommand failCommand =
                Assert.IsType<FailCityPopulationBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationValidationFailed,
                actual: failCommand.FailureCode);
            Assert.Equal(
                expected: "Failed",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: PopulationBootstrapFailureCodes.PopulationValidationFailed,
                actual: result.PopulationBootstrap.FailureCode);
        }

        [Fact]
        public async Task ProvisionAsync_WhenEconomyBootstrapTimesOut_MapsEconomyTimeout()
        {
            City city = ClassicCityTestSupport.CreateCity(
                name: "Timeout Economy City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityEconomyBootstrapCommand
                    ? true
                    : null
            };
            var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient
            {
                ExceptionToThrow = new OperationCanceledException("economy timed out")
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                economyClient: economyClient,
                populationClient: populationClient,
                supportsAutomaticPopulationBootstrap: true);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 25_000,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityEconomyBootstrapCommand failCommand = Assert.IsType<FailCityEconomyBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: EconomyBootstrapFailureCodes.EconomyTimeout,
                actual: failCommand.FailureCode);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Failed",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: EconomyBootstrapFailureCodes.EconomyTimeout,
                actual: result.EconomyBootstrap.FailureCode);
        }

        [Fact]
        public async Task ProvisionAsync_WhenEconomyBootstrapReturnsValidationError_MapsEconomyValidationFailure()
        {
            City city = ClassicCityTestSupport.CreateCity(
                name: "Validation Economy City",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is FailCityEconomyBootstrapCommand
                    ? true
                    : null
            };
            var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient
            {
                ExceptionToThrow = new HttpRequestException(
                    message: "economy validation failed",
                    inner: null,
                    statusCode: HttpStatusCode.BadRequest)
            };
            var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository
                {
                    CityById = city
                },
                economyClient: economyClient,
                populationClient: populationClient,
                supportsAutomaticPopulationBootstrap: true);

            CityProvisioningModel result = await orchestrator.ProvisionAsync(
                cityId: city.Id.Value,
                simulationKind: "ClassicCity",
                populationBootstrapOperationId: city.PopulationBootstrapOperationId,
                economyBootstrapOperationId: city.EconomyBootstrapOperationId,
                plannedPeopleCountOverride: 25_000,
                heartbeatAsync: null,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            FailCityEconomyBootstrapCommand failCommand = Assert.IsType<FailCityEconomyBootstrapCommand>(sentCommand);
            Assert.Equal(
                expected: EconomyBootstrapFailureCodes.EconomyValidationFailed,
                actual: failCommand.FailureCode);
            Assert.Null(populationClient.RequestedRequest);
            Assert.Equal(
                expected: "Failed",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: EconomyBootstrapFailureCodes.EconomyValidationFailed,
                actual: result.EconomyBootstrap.FailureCode);
        }

        private static ClassicCityProvisioningOrchestrator CreateOrchestrator(
            ProvisioningTestSupport.FakeMediator mediator,
            ClassicCityTestSupport.FakeCityRepository cityRepository,
            ProvisioningTestSupport.FakeCityEconomyBootstrapClient economyClient,
            ProvisioningTestSupport.FakeCityPopulationBootstrapClient populationClient,
            bool supportsAutomaticPopulationBootstrap)
        {
            return new ClassicCityProvisioningOrchestrator(
                mediator: mediator,
                cityRepository: cityRepository,
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository(),
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository(),
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository(),
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        Descriptor = new SimulationKindDescriptor(
                            Kind: SimulationKind.ClassicCity,
                            DisplayName: "Classic City",
                            Description: "Classic city simulation.",
                            SupportsAutomaticPopulationBootstrap: supportsAutomaticPopulationBootstrap)
                    },
                economyBootstrapClient: economyClient,
                populationBootstrapClient: populationClient,
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);
        }
    }
}
