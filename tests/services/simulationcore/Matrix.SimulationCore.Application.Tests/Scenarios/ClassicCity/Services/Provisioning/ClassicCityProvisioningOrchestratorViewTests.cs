using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Provisioning;

public sealed class ClassicCityProvisioningOrchestratorViewTests
{
    [Fact]
    public async Task GetProvisioningViewAsync_WhenCityDoesNotExist_ThrowsInvalidOperationException()
    {
        var orchestrator = CreateOrchestrator(
            cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
            supportsAutomaticPopulationBootstrap: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.GetProvisioningViewAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetProvisioningViewAsync_WhenAutomaticPopulationBootstrapIsUnsupported_ReturnsSkippedPopulationBootstrap()
    {
        var city = ClassicCityTestSupport.CreateCity(
            name: "Manual Population City",
            requiresEconomyBootstrap: true);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var orchestrator = CreateOrchestrator(
            cityRepository: cityRepository,
            supportsAutomaticPopulationBootstrap: false);

        var result = await orchestrator.GetProvisioningViewAsync(city.Id.Value, CancellationToken.None);

        Assert.Equal(city.Id.Value, result.CityId);
        Assert.Equal("ClassicCity", result.SimulationKind);
        Assert.Equal("Skipped", result.PopulationBootstrap.Status);
        Assert.Equal(city.PopulationBootstrapOperationId, result.PopulationBootstrap.OperationId);
        Assert.Equal(city.GenerationProfile.PlannedPeopleCount, result.PopulationBootstrap.PlannedPeopleCount);
        Assert.Null(result.PopulationBootstrap.FailureCode);
        Assert.Equal("Pending", result.EconomyBootstrap.Status);
        Assert.Equal(city.EconomyBootstrapOperationId, result.EconomyBootstrap.OperationId);
    }

    [Fact]
    public async Task CreateAsync_DelegatesToMediatorAndReturnsProvisioningView()
    {
        var city = ClassicCityTestSupport.CreateCity(
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
        var orchestrator = CreateOrchestrator(
            mediator: mediator,
            cityRepository: cityRepository,
            supportsAutomaticPopulationBootstrap: true);
        var command = new CreateCityCommand(
            Name: "Provisioned Neo Tokyo",
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
            StartSimTimeUtc: DateTimeOffset.Parse("2048-09-01T10:00:00+00:00"),
            SpeedMultiplier: 60m,
            PlannedPeopleCount: 25_000,
            ProvisioningCorrelationId: Guid.NewGuid(),
            ScenarioModelSetVersion: "classic-city-v3");

        var result = await orchestrator.CreateAsync(command, CancellationToken.None);

        var sentCommand = Assert.Single(mediator.SentRequests);
        Assert.Same(command, sentCommand);
        Assert.Equal(city.Id.Value, result.CityId);
        Assert.Equal(city.PopulationBootstrapOperationId, result.PopulationBootstrap.OperationId);
        Assert.Equal(city.EconomyBootstrapOperationId, result.EconomyBootstrap.OperationId);
        Assert.Equal("Pending", result.PopulationBootstrap.Status);
        Assert.Equal("Pending", result.EconomyBootstrap.Status);
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
        var orchestrator = CreateOrchestrator(
            mediator: mediator,
            cityRepository: new ClassicCityTestSupport.FakeCityRepository(),
            supportsAutomaticPopulationBootstrap: true);
        var command = new CreateCityCommand(
            Name: "Ghost City",
            SimulationKind: "ClassicCity",
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
            orchestrator.CreateAsync(command, CancellationToken.None));
    }

    private static ClassicCityProvisioningOrchestrator CreateOrchestrator(
        ProvisioningTestSupport.FakeMediator? mediator = null,
        ClassicCityTestSupport.FakeCityRepository? cityRepository = null,
        bool supportsAutomaticPopulationBootstrap = true)
    {
        return new ClassicCityProvisioningOrchestrator(
            mediator ?? new ProvisioningTestSupport.FakeMediator(),
            cityRepository ?? new ClassicCityTestSupport.FakeCityRepository(),
            new TopologyTestSupport.FakeCityAnchorRepository(),
            new TopologyTestSupport.FakeResidentialBuildingRepository(),
            new SimulationTestSupport.FakeSimulationClockRepository(),
            [
                new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                {
                    Descriptor = new SimulationKindDescriptor(
                        Kind: SimulationKind.ClassicCity,
                        DisplayName: "Classic City",
                        Description: "Classic city simulation.",
                        SupportsAutomaticPopulationBootstrap: supportsAutomaticPopulationBootstrap)
                }
            ],
            new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
            new ProvisioningTestSupport.FakeCityPopulationBootstrapClient(),
            NullLogger<ClassicCityProvisioningOrchestrator>.Instance);
    }
}
