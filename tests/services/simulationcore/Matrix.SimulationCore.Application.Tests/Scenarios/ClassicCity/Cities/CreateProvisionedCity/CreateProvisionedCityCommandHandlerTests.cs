using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateProvisionedCity;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.CreateProvisionedCity;

public sealed class CreateProvisionedCityCommandHandlerTests
{
    [Fact]
    public void PermissionKey_ForwardsInnerCityPermissionKey()
    {
        var cityCommand = CreateCityCommand();
        var command = new CreateProvisionedCityCommand(cityCommand);

        Assert.Equal(cityCommand.PermissionKey, command.PermissionKey);
    }

    [Fact]
    public async Task Handle_DelegatesToOrchestratorAndReturnsProvisioningView()
    {
        var cityCommand = CreateCityCommand();
        var expected = new CityProvisioningModel(
            CityId: Guid.NewGuid(),
            SimulationKind: "ClassicCity",
            PopulationBootstrap: new CityPopulationBootstrapModel(
                OperationId: Guid.NewGuid(),
                Status: "Provisioning",
                PlannedPeopleCount: 25_000,
                ResidentialCapacity: null,
                Summary: null,
                FailureCode: null),
            EconomyBootstrap: new CityEconomyBootstrapModel(
                OperationId: Guid.NewGuid(),
                Status: "Provisioning",
                FailureCode: null,
                UnitKind: null,
                UnitCode: null,
                UnitDisplayName: null,
                UnitSymbol: null));
        var orchestrator = new FakeClassicCityProvisioningOrchestrator
        {
            Result = expected
        };
        var handler = new CreateProvisionedCityCommandHandler(orchestrator);

        var result = await handler.Handle(new CreateProvisionedCityCommand(cityCommand), CancellationToken.None);

        Assert.Same(cityCommand, orchestrator.RequestedRequest);
        Assert.Equal(expected, result);
    }

    private static CreateCityCommand CreateCityCommand()
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
            ProvisioningCorrelationId: Guid.NewGuid(),
            ScenarioModelSetVersion: "classic-city-v3");
    }

    private sealed class FakeClassicCityProvisioningOrchestrator : IClassicCityProvisioningOrchestrator
    {
        public CreateCityCommand? RequestedRequest { get; private set; }
        public required CityProvisioningModel Result { get; init; }

        public Task<CityProvisioningModel> CreateAsync(CreateCityCommand request, CancellationToken cancellationToken)
        {
            RequestedRequest = request;
            return Task.FromResult(Result);
        }

        public Task<CityProvisioningModel> GetProvisioningViewAsync(Guid cityId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<CityProvisioningModel> ProvisionAsync(
            Guid cityId,
            string simulationKind,
            Guid populationBootstrapOperationId,
            Guid economyBootstrapOperationId,
            int? plannedPeopleCountOverride,
            Func<CancellationToken, Task>? heartbeatAsync,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
