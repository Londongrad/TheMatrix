using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityTests
    {
        private const string CityArchivedErrorCode = "SimulationCore.City.Archived";

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2040,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            offset: TimeSpan.Zero);

        private static readonly DateTimeOffset ArchivedAtUtc = new(
            year: 2040,
            month: 2,
            day: 4,
            hour: 5,
            minute: 6,
            second: 7,
            offset: TimeSpan.Zero);

        [Fact]
        public void Create_WhenBootstrapIsNotRequired_SetsActiveState_AndEmitsCreatedEvent()
        {
            var name = new CityName("Alpha City");
            CityEnvironment environment = CreateEnvironment();
            var generationSeed = new CityGenerationSeed("alpha-seed");
            var scenarioModelSetVersion = new ScenarioModelSetVersion("classic-city-v3");
            CityGenerationProfile generationProfile = CreateGenerationProfile();
            CityInitialWeatherProfile initialWeatherProfile = CreateInitialWeatherProfile();

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

            Assert.Equal(
                expected: name,
                actual: city.Name);
            Assert.Equal(
                expected: SimulationKind.ClassicCity,
                actual: city.SimulationKind);
            Assert.Equal(
                expected: environment,
                actual: city.Environment);
            Assert.Equal(
                expected: generationSeed,
                actual: city.GenerationSeed);
            Assert.Equal(
                expected: scenarioModelSetVersion,
                actual: city.ScenarioModelSetVersion);
            Assert.Equal(
                expected: generationProfile,
                actual: city.GenerationProfile);
            Assert.Equal(
                expected: initialWeatherProfile,
                actual: city.InitialWeatherProfile);
            Assert.Equal(
                expected: CityStatus.Active,
                actual: city.Status);
            Assert.True(city.IsActive);
            Assert.False(city.IsProvisioning);
            Assert.False(city.IsArchived);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: city.CreatedAtUtc);
            Assert.NotEqual(
                expected: Guid.Empty,
                actual: city.Id.Value);
            Assert.NotEqual(
                expected: Guid.Empty,
                actual: city.RunId);
            Assert.NotEqual(
                expected: Guid.Empty,
                actual: city.PopulationBootstrapOperationId);
            Assert.NotEqual(
                expected: Guid.Empty,
                actual: city.EconomyBootstrapOperationId);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: city.PopulationBootstrapCompletedAtUtc);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: city.EconomyBootstrapCompletedAtUtc);
            Assert.Null(city.PopulationBootstrapFailedAtUtc);
            Assert.Null(city.EconomyBootstrapFailedAtUtc);
            Assert.Null(city.PopulationBootstrapFailureCode);
            Assert.Null(city.EconomyBootstrapFailureCode);
            Assert.Null(city.ProvisioningStartedAtUtc);
            Assert.Null(city.ProvisioningHeartbeatAtUtc);
            Assert.Null(city.ProvisioningLeaseExpiresAtUtc);
            Assert.Equal(
                expected: 0,
                actual: city.ProvisioningAttemptCount);
            Assert.Null(city.ArchivedAtUtc);

            CityCreatedDomainEvent createdEvent =
                Assert.IsType<CityCreatedDomainEvent>(Assert.Single(city.DomainEvents));

            Assert.Equal(
                expected: city.Id,
                actual: createdEvent.CityId);
            Assert.Equal(
                expected: name,
                actual: createdEvent.Name);
            Assert.Equal(
                expected: SimulationKind.ClassicCity,
                actual: createdEvent.SimulationKind);
            Assert.Equal(
                expected: environment,
                actual: createdEvent.Environment);
            Assert.Equal(
                expected: generationSeed,
                actual: createdEvent.GenerationSeed);
            Assert.Equal(
                expected: city.RunId,
                actual: createdEvent.RunId);
            Assert.Equal(
                expected: scenarioModelSetVersion,
                actual: createdEvent.ScenarioModelSetVersion);
            Assert.Equal(
                expected: generationProfile,
                actual: createdEvent.GenerationProfile);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: createdEvent.PopulationBootstrapOperationId);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: createdEvent.CreatedAtUtc);
        }

        [Fact]
        public void Create_WhenBootstrapIsRequired_SetsProvisioningState_AndProvisioningTimestamps()
        {
            City city = CreateCity(
                requiresPopulationBootstrap: true,
                requiresEconomyBootstrap: false);

            Assert.Equal(
                expected: CityStatus.Provisioning,
                actual: city.Status);
            Assert.False(city.IsActive);
            Assert.True(city.IsProvisioning);
            Assert.Null(city.PopulationBootstrapCompletedAtUtc);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: city.EconomyBootstrapCompletedAtUtc);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: city.ProvisioningStartedAtUtc);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: city.ProvisioningHeartbeatAtUtc);
        }

        [Fact]
        public void Rename_WhenNameChanges_UpdatesName_AndEmitsEvent()
        {
            City city = CreateCity();
            var newName = new CityName("Beta City");

            city.ClearDomainEvents();
            city.Rename(newName);

            Assert.Equal(
                expected: newName,
                actual: city.Name);

            CityRenamedDomainEvent renamedEvent =
                Assert.IsType<CityRenamedDomainEvent>(Assert.Single(city.DomainEvents));

            Assert.Equal(
                expected: city.Id,
                actual: renamedEvent.CityId);
            Assert.Equal(
                expected: new CityName("Alpha City"),
                actual: renamedEvent.From);
            Assert.Equal(
                expected: newName,
                actual: renamedEvent.To);
        }

        [Fact]
        public void Rename_WithSameName_IsNoOp()
        {
            City city = CreateCity();

            city.ClearDomainEvents();
            city.Rename(new CityName("Alpha City"));

            Assert.Equal(
                expected: new CityName("Alpha City"),
                actual: city.Name);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void Rename_WhenArchived_ThrowsDomainException()
        {
            City city = CreateCity();
            city.Archive(ArchivedAtUtc);
            city.ClearDomainEvents();

            DomainException exception = Assert.Throws<DomainException>(() => city.Rename(new CityName("Gamma City")));

            Assert.Equal(
                expected: CityArchivedErrorCode,
                actual: exception.Code);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void ChangeEnvironment_WhenValueChanges_UpdatesEnvironment_AndEmitsEvent()
        {
            City city = CreateCity();
            var newEnvironment = CityEnvironment.Create(
                climateZone: ClimateZone.Arid,
                hemisphere: Hemisphere.Southern,
                utcOffset: CityUtcOffset.FromMinutes(600));

            city.ClearDomainEvents();
            city.ChangeEnvironment(newEnvironment);

            Assert.Equal(
                expected: newEnvironment,
                actual: city.Environment);

            CityEnvironmentChangedDomainEvent changedEvent =
                Assert.IsType<CityEnvironmentChangedDomainEvent>(Assert.Single(city.DomainEvents));

            Assert.Equal(
                expected: city.Id,
                actual: changedEvent.CityId);
            Assert.Equal(
                expected: CreateEnvironment(),
                actual: changedEvent.From);
            Assert.Equal(
                expected: newEnvironment,
                actual: changedEvent.To);
        }

        [Fact]
        public void ChangeEnvironment_WithSameValue_IsNoOp()
        {
            City city = CreateCity();

            city.ClearDomainEvents();
            city.ChangeEnvironment(CreateEnvironment());

            Assert.Equal(
                expected: CreateEnvironment(),
                actual: city.Environment);
            Assert.Empty(city.DomainEvents);
        }

        [Fact]
        public void ChangeEnvironment_WhenArchived_ThrowsDomainException()
        {
            City city = CreateCity();
            city.Archive(ArchivedAtUtc);
            city.ClearDomainEvents();

            DomainException exception =
                Assert.Throws<DomainException>(() => city.ChangeEnvironment(CreateAlternativeEnvironment()));

            Assert.Equal(
                expected: CityArchivedErrorCode,
                actual: exception.Code);
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
}
