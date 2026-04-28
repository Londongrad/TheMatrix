using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
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
