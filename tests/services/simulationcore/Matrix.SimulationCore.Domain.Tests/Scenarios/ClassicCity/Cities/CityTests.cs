using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

public sealed class CityTests
{
    private const string CityArchivedErrorCode = "SimulationCore.City.Archived";
    private static readonly DateTimeOffset CreatedAtUtc = new(2040, 2, 3, 4, 5, 6, TimeSpan.Zero);
    private static readonly DateTimeOffset ArchivedAtUtc = new(2040, 2, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    public void Create_WhenBootstrapIsNotRequired_SetsActiveState_AndEmitsCreatedEvent()
    {
        var name = new CityName("Alpha City");
        var environment = CreateEnvironment();
        var generationSeed = new CityGenerationSeed("alpha-seed");
        var scenarioModelSetVersion = new ScenarioModelSetVersion("classic-city-v3");
        var generationProfile = CreateGenerationProfile();
        var initialWeatherProfile = CreateInitialWeatherProfile();

        var city = City.Create(
            name: name,
            simulationKind: SimulationKind.ClassicCity,
            environment: environment,
            generationSeed: generationSeed,
            scenarioModelSetVersion: scenarioModelSetVersion,
            generationProfile: generationProfile,
            initialWeatherProfile: initialWeatherProfile,
            provisioningCorrelationId: null,
            requiresPopulationBootstrap: false,
            requiresEconomyBootstrap: false,
            createdAtUtc: CreatedAtUtc);

        Assert.Equal(name, city.Name);
        Assert.Equal(SimulationKind.ClassicCity, city.SimulationKind);
        Assert.Equal(environment, city.Environment);
        Assert.Equal(generationSeed, city.GenerationSeed);
        Assert.Equal(scenarioModelSetVersion, city.ScenarioModelSetVersion);
        Assert.Equal(generationProfile, city.GenerationProfile);
        Assert.Equal(initialWeatherProfile, city.InitialWeatherProfile);
        Assert.Equal(CityStatus.Active, city.Status);
        Assert.True(city.IsActive);
        Assert.False(city.IsProvisioning);
        Assert.False(city.IsArchived);
        Assert.Equal(CreatedAtUtc, city.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, city.Id.Value);
        Assert.NotEqual(Guid.Empty, city.RunId);
        Assert.NotEqual(Guid.Empty, city.PopulationBootstrapOperationId);
        Assert.NotEqual(Guid.Empty, city.EconomyBootstrapOperationId);
        Assert.Equal(CreatedAtUtc, city.PopulationBootstrapCompletedAtUtc);
        Assert.Equal(CreatedAtUtc, city.EconomyBootstrapCompletedAtUtc);
        Assert.Null(city.PopulationBootstrapFailedAtUtc);
        Assert.Null(city.EconomyBootstrapFailedAtUtc);
        Assert.Null(city.PopulationBootstrapFailureCode);
        Assert.Null(city.EconomyBootstrapFailureCode);
        Assert.Null(city.ProvisioningStartedAtUtc);
        Assert.Null(city.ProvisioningHeartbeatAtUtc);
        Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
        Assert.Equal(0, city.ProvisioningAttemptCount);
        Assert.Null(city.ArchivedAtUtc);

        var createdEvent = Assert.IsType<CityCreatedDomainEvent>(Assert.Single(city.DomainEvents));

        Assert.Equal(city.Id, createdEvent.CityId);
        Assert.Equal(name, createdEvent.Name);
        Assert.Equal(SimulationKind.ClassicCity, createdEvent.SimulationKind);
        Assert.Equal(environment, createdEvent.Environment);
        Assert.Equal(generationSeed, createdEvent.GenerationSeed);
        Assert.Equal(city.RunId, createdEvent.RunId);
        Assert.Equal(scenarioModelSetVersion, createdEvent.ScenarioModelSetVersion);
        Assert.Equal(generationProfile, createdEvent.GenerationProfile);
        Assert.Equal(city.PopulationBootstrapOperationId, createdEvent.PopulationBootstrapOperationId);
        Assert.Equal(CreatedAtUtc, createdEvent.CreatedAtUtc);
    }

    [Fact]
    public void Create_WhenBootstrapIsRequired_SetsProvisioningState_AndProvisioningTimestamps()
    {
        var city = CreateCity(
            requiresPopulationBootstrap: true,
            requiresEconomyBootstrap: false);

        Assert.Equal(CityStatus.Provisioning, city.Status);
        Assert.False(city.IsActive);
        Assert.True(city.IsProvisioning);
        Assert.Null(city.PopulationBootstrapCompletedAtUtc);
        Assert.Equal(CreatedAtUtc, city.EconomyBootstrapCompletedAtUtc);
        Assert.Equal(CreatedAtUtc, city.ProvisioningStartedAtUtc);
        Assert.Equal(CreatedAtUtc, city.ProvisioningHeartbeatAtUtc);
    }

    [Fact]
    public void Rename_WhenNameChanges_UpdatesName_AndEmitsEvent()
    {
        var city = CreateCity();
        var newName = new CityName("Beta City");

        city.ClearDomainEvents();
        city.Rename(newName);

        Assert.Equal(newName, city.Name);

        var renamedEvent = Assert.IsType<CityRenamedDomainEvent>(Assert.Single(city.DomainEvents));

        Assert.Equal(city.Id, renamedEvent.CityId);
        Assert.Equal(new CityName("Alpha City"), renamedEvent.From);
        Assert.Equal(newName, renamedEvent.To);
    }

    [Fact]
    public void Rename_WithSameName_IsNoOp()
    {
        var city = CreateCity();

        city.ClearDomainEvents();
        city.Rename(new CityName("Alpha City"));

        Assert.Equal(new CityName("Alpha City"), city.Name);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void Rename_WhenArchived_ThrowsDomainException()
    {
        var city = CreateCity();
        city.Archive(ArchivedAtUtc);
        city.ClearDomainEvents();

        var exception = Assert.Throws<DomainException>(() => city.Rename(new CityName("Gamma City")));

        Assert.Equal(CityArchivedErrorCode, exception.Code);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void ChangeEnvironment_WhenValueChanges_UpdatesEnvironment_AndEmitsEvent()
    {
        var city = CreateCity();
        var newEnvironment = CityEnvironment.Create(
            climateZone: ClimateZone.Arid,
            hemisphere: Hemisphere.Southern,
            utcOffset: CityUtcOffset.FromMinutes(600));

        city.ClearDomainEvents();
        city.ChangeEnvironment(newEnvironment);

        Assert.Equal(newEnvironment, city.Environment);

        var changedEvent = Assert.IsType<CityEnvironmentChangedDomainEvent>(Assert.Single(city.DomainEvents));

        Assert.Equal(city.Id, changedEvent.CityId);
        Assert.Equal(CreateEnvironment(), changedEvent.From);
        Assert.Equal(newEnvironment, changedEvent.To);
    }

    [Fact]
    public void ChangeEnvironment_WithSameValue_IsNoOp()
    {
        var city = CreateCity();

        city.ClearDomainEvents();
        city.ChangeEnvironment(CreateEnvironment());

        Assert.Equal(CreateEnvironment(), city.Environment);
        Assert.Empty(city.DomainEvents);
    }

    [Fact]
    public void ChangeEnvironment_WhenArchived_ThrowsDomainException()
    {
        var city = CreateCity();
        city.Archive(ArchivedAtUtc);
        city.ClearDomainEvents();

        var exception = Assert.Throws<DomainException>(() => city.ChangeEnvironment(CreateAlternativeEnvironment()));

        Assert.Equal(CityArchivedErrorCode, exception.Code);
        Assert.Empty(city.DomainEvents);
    }

    private static City CreateCity(
        bool requiresPopulationBootstrap = false,
        bool requiresEconomyBootstrap = false)
    {
        return City.Create(
            name: new CityName("Alpha City"),
            simulationKind: SimulationKind.ClassicCity,
            environment: CreateEnvironment(),
            generationSeed: new CityGenerationSeed("alpha-seed"),
            scenarioModelSetVersion: new ScenarioModelSetVersion("classic-city-v3"),
            generationProfile: CreateGenerationProfile(),
            initialWeatherProfile: CreateInitialWeatherProfile(),
            provisioningCorrelationId: null,
            requiresPopulationBootstrap: requiresPopulationBootstrap,
            requiresEconomyBootstrap: requiresEconomyBootstrap,
            createdAtUtc: CreatedAtUtc);
    }

    private static CityEnvironment CreateEnvironment()
    {
        return CityEnvironment.Create(
            climateZone: ClimateZone.Temperate,
            hemisphere: Hemisphere.Northern,
            utcOffset: CityUtcOffset.FromMinutes(180));
    }

    private static CityEnvironment CreateAlternativeEnvironment()
    {
        return CityEnvironment.Create(
            climateZone: ClimateZone.Arid,
            hemisphere: Hemisphere.Southern,
            utcOffset: CityUtcOffset.FromMinutes(600));
    }

    private static CityGenerationProfile CreateGenerationProfile()
    {
        return CityGenerationProfile.Create(
            sizeTier: CitySizeTier.Medium,
            urbanDensity: UrbanDensity.Balanced,
            developmentLevel: CityDevelopmentLevel.Balanced,
            economyProfile: CityEconomyProfile.Balanced,
            populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
            plannedPeopleCount: 25_000);
    }

    private static CityInitialWeatherProfile CreateInitialWeatherProfile()
    {
        return CityInitialWeatherProfile.CreateManual(
            manualType: WeatherType.Clear,
            manualSeverity: WeatherSeverity.Calm,
            manualTemperature: TemperatureC.From(18m));
    }
}
