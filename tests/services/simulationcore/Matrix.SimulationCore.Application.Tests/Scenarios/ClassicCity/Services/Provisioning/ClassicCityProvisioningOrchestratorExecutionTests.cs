using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Matrix.SimulationCore.Application.Services.Bootstrap;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Provisioning;

public sealed class ClassicCityProvisioningOrchestratorExecutionTests
{
    [Fact]
    public async Task ProvisionAsync_WhenCityDoesNotExist_ThrowsInvalidOperationException()
    {
        var orchestrator = CreateOrchestrator(
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
    public async Task ProvisionAsync_WhenEconomyCompletesAndPopulationBootstrapIsUnsupported_CompletesEconomyAndSkipsPopulation()
    {
        int heartbeatCallCount = 0;
        var city = ClassicCityTestSupport.CreateCity(
            name: "Manual Population City",
            requiresEconomyBootstrap: true);
        var mediator = new ProvisioningTestSupport.FakeMediator
        {
            SendHandler = request => request is CompleteCityEconomyBootstrapCommand ? true : null
        };
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient
        {
            Result = new Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions.CityEconomyBootstrapResult(
                UnitKind: "Currency",
                UnitCode: "NCR",
                UnitDisplayName: "Neo Credits",
                UnitSymbol: "N$")
        };
        var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
        var orchestrator = CreateOrchestrator(
            mediator: mediator,
            cityRepository: cityRepository,
            economyClient: economyClient,
            populationClient: populationClient,
            supportsAutomaticPopulationBootstrap: false);

        var result = await orchestrator.ProvisionAsync(
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

        Assert.Equal(1, heartbeatCallCount);
        Assert.Equal(city.Id.Value, economyClient.RequestedCityId);
        Assert.Equal("ClassicCity", economyClient.RequestedSimulationKind);
        Assert.Equal(city.GenerationProfile.EconomyProfile.ToString(), economyClient.RequestedEconomyProfile);
        var sentCommand = Assert.Single(mediator.SentRequests);
        var completeCommand = Assert.IsType<CompleteCityEconomyBootstrapCommand>(sentCommand);
        Assert.Equal(city.Id.Value, completeCommand.CityId);
        Assert.Equal(city.EconomyBootstrapOperationId, completeCommand.OperationId);
        Assert.Null(populationClient.RequestedRequest);
        Assert.Equal("Completed", result.EconomyBootstrap.Status);
        Assert.Equal("NCR", result.EconomyBootstrap.UnitCode);
        Assert.Equal("Skipped", result.PopulationBootstrap.Status);
        Assert.Equal(12_345, result.PopulationBootstrap.PlannedPeopleCount);
    }

    [Fact]
    public async Task ProvisionAsync_WhenEconomyBootstrapFails_ReturnsFailureAndDoesNotRunPopulationBootstrap()
    {
        var city = ClassicCityTestSupport.CreateCity(
            name: "Broken Economy City",
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        var mediator = new ProvisioningTestSupport.FakeMediator
        {
            SendHandler = request => request is FailCityEconomyBootstrapCommand ? true : null
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
        var orchestrator = CreateOrchestrator(
            mediator: mediator,
            cityRepository: cityRepository,
            economyClient: economyClient,
            populationClient: populationClient,
            supportsAutomaticPopulationBootstrap: true);

        var result = await orchestrator.ProvisionAsync(
            cityId: city.Id.Value,
            simulationKind: "ClassicCity",
            populationBootstrapOperationId: city.PopulationBootstrapOperationId,
            economyBootstrapOperationId: city.EconomyBootstrapOperationId,
            plannedPeopleCountOverride: 25_000,
            heartbeatAsync: null,
            cancellationToken: CancellationToken.None);

        var sentCommand = Assert.Single(mediator.SentRequests);
        var failCommand = Assert.IsType<FailCityEconomyBootstrapCommand>(sentCommand);
        Assert.Equal(city.Id.Value, failCommand.CityId);
        Assert.Equal(city.EconomyBootstrapOperationId, failCommand.OperationId);
        Assert.Equal(EconomyBootstrapFailureCodes.EconomyConflict, failCommand.FailureCode);
        Assert.Null(populationClient.RequestedRequest);
        Assert.Equal("Failed", result.EconomyBootstrap.Status);
        Assert.Equal(EconomyBootstrapFailureCodes.EconomyConflict, result.EconomyBootstrap.FailureCode);
        Assert.Equal("Pending", result.PopulationBootstrap.Status);
        Assert.Equal(25_000, result.PopulationBootstrap.PlannedPeopleCount);
    }

    [Fact]
    public async Task ProvisionAsync_WhenCityIsAlreadyActive_ReturnsCompletedPopulationWithoutCallingPopulationBootstrapClient()
    {
        var city = ClassicCityTestSupport.CreateCity("Active City");
        var mediator = new ProvisioningTestSupport.FakeMediator();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var economyClient = new ProvisioningTestSupport.FakeCityEconomyBootstrapClient();
        var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
        var orchestrator = CreateOrchestrator(
            mediator: mediator,
            cityRepository: cityRepository,
            economyClient: economyClient,
            populationClient: populationClient,
            supportsAutomaticPopulationBootstrap: true);

        var result = await orchestrator.ProvisionAsync(
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
        Assert.Equal("Completed", result.EconomyBootstrap.Status);
        Assert.Equal("Completed", result.PopulationBootstrap.Status);
        Assert.Equal(city.GenerationProfile.PlannedPeopleCount, result.PopulationBootstrap.PlannedPeopleCount);
    }

    [Fact]
    public async Task ProvisionAsync_WhenPopulationBootstrapAlreadyFailed_ReturnsFailedPopulationStateWithoutCallingPopulationClient()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity(
            name: "Population Failure City",
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        Assert.True(city.TryCompleteEconomyBootstrap(city.EconomyBootstrapOperationId, failedAtUtc.AddMinutes(-5)));
        Assert.True(city.TryFailPopulationBootstrap(
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
        var orchestrator = CreateOrchestrator(
            mediator: mediator,
            cityRepository: cityRepository,
            economyClient: economyClient,
            populationClient: populationClient,
            supportsAutomaticPopulationBootstrap: true);

        var result = await orchestrator.ProvisionAsync(
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
        Assert.Equal("Completed", result.EconomyBootstrap.Status);
        Assert.Equal("Failed", result.PopulationBootstrap.Status);
        Assert.Equal("POPULATION_CONFLICT", result.PopulationBootstrap.FailureCode);
        Assert.Equal(20_000, result.PopulationBootstrap.PlannedPeopleCount);
    }

    [Fact]
    public async Task ProvisionAsync_WhenResidentialCapacityIsMissing_FailsPopulationBootstrapWithoutCallingPopulationClient()
    {
        DateTimeOffset completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity(
            name: "No Capacity City",
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        Assert.True(city.TryCompleteEconomyBootstrap(city.EconomyBootstrapOperationId, completedAtUtc));
        var mediator = new ProvisioningTestSupport.FakeMediator
        {
            SendHandler = request => request is FailCityPopulationBootstrapCommand ? true : null
        };
        var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
        var orchestrator = new ClassicCityProvisioningOrchestrator(
            mediator,
            new ClassicCityTestSupport.FakeCityRepository { CityById = city },
            new TopologyTestSupport.FakeCityAnchorRepository(),
            new TopologyTestSupport.FakeResidentialBuildingRepository(),
            new SimulationTestSupport.FakeSimulationClockRepository
            {
                ClockBySimulationId = SimulationTestSupport.CreateClock(city.Id.Value)
            },
            [
                new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                {
                    Descriptor = new SimulationKindDescriptor(
                        Kind: SimulationKind.ClassicCity,
                        DisplayName: "Classic City",
                        Description: "Classic city simulation.",
                        SupportsAutomaticPopulationBootstrap: true)
                }
            ],
            new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
            populationClient,
            NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

        var result = await orchestrator.ProvisionAsync(
            cityId: city.Id.Value,
            simulationKind: "ClassicCity",
            populationBootstrapOperationId: city.PopulationBootstrapOperationId,
            economyBootstrapOperationId: city.EconomyBootstrapOperationId,
            plannedPeopleCountOverride: 12_345,
            heartbeatAsync: null,
            cancellationToken: CancellationToken.None);

        var sentCommand = Assert.Single(mediator.SentRequests);
        var failCommand = Assert.IsType<FailCityPopulationBootstrapCommand>(sentCommand);
        Assert.Equal(city.Id.Value, failCommand.CityId);
        Assert.Equal(city.PopulationBootstrapOperationId, failCommand.OperationId);
        Assert.Equal(PopulationBootstrapFailureCodes.PopulationResidentialCapacityMissing, failCommand.FailureCode);
        Assert.Null(populationClient.RequestedRequest);
        Assert.Equal("Completed", result.EconomyBootstrap.Status);
        Assert.Equal("Failed", result.PopulationBootstrap.Status);
        Assert.Equal(12_345, result.PopulationBootstrap.PlannedPeopleCount);
        Assert.Equal(0, result.PopulationBootstrap.ResidentialCapacity);
        Assert.Equal(PopulationBootstrapFailureCodes.PopulationResidentialCapacityMissing, result.PopulationBootstrap.FailureCode);
    }

    [Fact]
    public async Task ProvisionAsync_WhenSimulationClockIsMissing_FailsPopulationBootstrapWithInvalidResponseCode()
    {
        DateTimeOffset completedAtUtc = DateTimeOffset.Parse("2048-10-01T12:00:00+00:00");
        var city = ClassicCityTestSupport.CreateCity(
            name: "Clockless City",
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: true);
        Assert.True(city.TryCompleteEconomyBootstrap(city.EconomyBootstrapOperationId, completedAtUtc));
        var district = TopologyTestSupport.CreateDistrict(city.Id, "Downtown");
        var building = TopologyTestSupport.CreateResidentialBuilding(city.Id, district.Id, "River Tower");
        var mediator = new ProvisioningTestSupport.FakeMediator
        {
            SendHandler = request => request is FailCityPopulationBootstrapCommand ? true : null
        };
        var populationClient = new ProvisioningTestSupport.FakeCityPopulationBootstrapClient();
        var orchestrator = new ClassicCityProvisioningOrchestrator(
            mediator,
            new ClassicCityTestSupport.FakeCityRepository { CityById = city },
            new TopologyTestSupport.FakeCityAnchorRepository(),
            new TopologyTestSupport.FakeResidentialBuildingRepository
            {
                Buildings = [building]
            },
            new SimulationTestSupport.FakeSimulationClockRepository(),
            [
                new ClassicCityTestSupport.FakeCitySimulationBootstrapStrategy
                {
                    Descriptor = new SimulationKindDescriptor(
                        Kind: SimulationKind.ClassicCity,
                        DisplayName: "Classic City",
                        Description: "Classic city simulation.",
                        SupportsAutomaticPopulationBootstrap: true)
                }
            ],
            new ProvisioningTestSupport.FakeCityEconomyBootstrapClient(),
            populationClient,
            NullLogger<ClassicCityProvisioningOrchestrator>.Instance);

        var result = await orchestrator.ProvisionAsync(
            cityId: city.Id.Value,
            simulationKind: "ClassicCity",
            populationBootstrapOperationId: city.PopulationBootstrapOperationId,
            economyBootstrapOperationId: city.EconomyBootstrapOperationId,
            plannedPeopleCountOverride: null,
            heartbeatAsync: null,
            cancellationToken: CancellationToken.None);

        var sentCommand = Assert.Single(mediator.SentRequests);
        var failCommand = Assert.IsType<FailCityPopulationBootstrapCommand>(sentCommand);
        Assert.Equal(PopulationBootstrapFailureCodes.PopulationResponseInvalid, failCommand.FailureCode);
        Assert.Null(populationClient.RequestedRequest);
        Assert.Equal("Failed", result.PopulationBootstrap.Status);
        Assert.Equal(PopulationBootstrapFailureCodes.PopulationResponseInvalid, result.PopulationBootstrap.FailureCode);
    }

    private static ClassicCityProvisioningOrchestrator CreateOrchestrator(
        ProvisioningTestSupport.FakeMediator mediator,
        ClassicCityTestSupport.FakeCityRepository cityRepository,
        ProvisioningTestSupport.FakeCityEconomyBootstrapClient economyClient,
        ProvisioningTestSupport.FakeCityPopulationBootstrapClient populationClient,
        bool supportsAutomaticPopulationBootstrap)
    {
        return new ClassicCityProvisioningOrchestrator(
            mediator,
            cityRepository,
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
            economyClient,
            populationClient,
            NullLogger<ClassicCityProvisioningOrchestrator>.Instance);
    }
}
