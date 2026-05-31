using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Provisioning
{
    public sealed class ClassicCityProvisioningOrchestratorViewTests
    {
        [Fact]
        public async Task GetProvisioningViewAsync_WhenCityDoesNotExist_ThrowsInvalidOperationException()
        {
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                supportsAutomaticPopulationBootstrap: true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                orchestrator.GetProvisioningViewAsync(
                    cityId: Guid.NewGuid(),
                    cancellationToken: CancellationToken.None));
        }

        [Fact]
        public async Task
            GetProvisioningViewAsync_WhenAutomaticPopulationBootstrapIsUnsupported_ReturnsSkippedPopulationBootstrap()
        {
            City city = ClassicCityTestSupport.CreateCity(
                name: "Manual Population City",
                requiresEconomyBootstrap: true);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                cityRepository: cityRepository,
                supportsAutomaticPopulationBootstrap: false);

            CityProvisioningModel result = await orchestrator.GetProvisioningViewAsync(
                cityId: city.Id.Value,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: city.Id.Value,
                actual: result.CityId);
            Assert.Equal(
                expected: "ClassicCity",
                actual: result.SimulationKind);
            Assert.Equal(
                expected: "Skipped",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: result.PopulationBootstrap.OperationId);
            Assert.Equal(
                expected: city.GenerationProfile.PlannedPeopleCount,
                actual: result.PopulationBootstrap.PlannedPeopleCount);
            Assert.Null(result.PopulationBootstrap.FailureCode);
            Assert.Equal(
                expected: "Pending",
                actual: result.EconomyBootstrap.Status);
            Assert.Equal(
                expected: city.EconomyBootstrapOperationId,
                actual: result.EconomyBootstrap.OperationId);
        }

        [Fact]
        public async Task CreateAsync_DelegatesToMediatorAndReturnsProvisioningView()
        {
            City city = ClassicCityTestSupport.CreateCity(
                name: "Provisioned Neo Tokyo",
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: true);
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is CreateCityCommand
                    ? new CityCreatedDto(
                        CityId: city.Id.Value,
                        PopulationBootstrapOperationId: city.PopulationBootstrapOperationId,
                        EconomyBootstrapOperationId: city.EconomyBootstrapOperationId,
                        SimulationKind: "ClassicCity")
                    : null
            };
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: cityRepository,
                supportsAutomaticPopulationBootstrap: true);
            var command = new CreateCityCommand(
                Name: "Provisioned Neo Tokyo",
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
                StartSimTimeUtc: DateTimeOffset.Parse("2048-09-01T10:00:00+00:00"),
                SpeedMultiplier: 60m,
                PlannedPeopleCount: 25_000,
                ProvisioningCorrelationId: Guid.NewGuid(),
                ScenarioModelSetVersion: "classic-city-v3");

            CityProvisioningModel result = await orchestrator.CreateAsync(
                request: command,
                cancellationToken: CancellationToken.None);

            object sentCommand = Assert.Single(mediator.SentRequests);
            Assert.Same(
                expected: command,
                actual: sentCommand);
            Assert.Equal(
                expected: city.Id.Value,
                actual: result.CityId);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: result.PopulationBootstrap.OperationId);
            Assert.Equal(
                expected: city.EconomyBootstrapOperationId,
                actual: result.EconomyBootstrap.OperationId);
            Assert.Equal(
                expected: "Pending",
                actual: result.PopulationBootstrap.Status);
            Assert.Equal(
                expected: "Pending",
                actual: result.EconomyBootstrap.Status);
        }

        [Fact]
        public async Task CreateAsync_WhenCreatedCityCannotBeLoaded_ThrowsInvalidOperationException()
        {
            var mediator = new ProvisioningTestSupport.FakeMediator
            {
                SendHandler = request => request is CreateCityCommand
                    ? new CityCreatedDto(
                        CityId: Guid.NewGuid(),
                        PopulationBootstrapOperationId: Guid.NewGuid(),
                        EconomyBootstrapOperationId: Guid.NewGuid(),
                        SimulationKind: "ClassicCity")
                    : null
            };
            ClassicCityProvisioningOrchestrator orchestrator = CreateOrchestrator(
                mediator: mediator,
                cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
                supportsAutomaticPopulationBootstrap: true);
            var command = new CreateCityCommand(
                Name: "Ghost City",
                ClimateZone: "Temperate",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180,
                GenerationSeed: "ghost-seed",
                SizeTier: "Medium",
                UrbanDensity: "Balanced",
                DevelopmentLevel: "Balanced",
                EconomyProfile: "Balanced",
                PopulationOccupancyProfile: "Balanced",
                InitialWeatherMode: "Manual",
                InitialWeatherType: "Clear",
                InitialWeatherSeverity: "Calm",
                InitialWeatherTemperatureC: 18m,
                StartSimTimeUtc: DateTimeOffset.Parse("2048-09-01T10:00:00+00:00"),
                SpeedMultiplier: 60m,
                PlannedPeopleCount: 25_000,
                ProvisioningCorrelationId: Guid.NewGuid(),
                ScenarioModelSetVersion: "classic-city-v3");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                orchestrator.CreateAsync(
                    request: command,
                    cancellationToken: CancellationToken.None));
        }

        private static ClassicCityProvisioningOrchestrator CreateOrchestrator(
            ProvisioningTestSupport.FakeMediator? mediator = null,
            ClassicCityTestSupport.FakeCityRepository? cityRepository = null,
            bool supportsAutomaticPopulationBootstrap = true)
        {
            return new ClassicCityProvisioningOrchestrator(
                mediator: mediator ?? new ProvisioningTestSupport.FakeMediator(),
                cityRepository: cityRepository ?? new ClassicCityTestSupport.FakeCityRepository(),
                cityAnchorRepository: new TopologyTestSupport.FakeCityAnchorRepository(),
                residentialBuildingRepository: new TopologyTestSupport.FakeResidentialBuildingRepository(),
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository(),
                simulationBootstrapStrategy:
                    new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                    {
                        SupportsAutomaticPopulationBootstrap = supportsAutomaticPopulationBootstrap
                    },
                economyBootstrapClient: new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
                populationBootstrapClient: new ProvisioningTestSupport.FakeCityPopulationBootstrapClient(),
                logger: NullLogger<ClassicCityProvisioningOrchestrator>.Instance);
        }
    }
}
