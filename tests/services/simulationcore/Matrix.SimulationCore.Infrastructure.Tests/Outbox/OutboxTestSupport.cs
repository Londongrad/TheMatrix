using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Tests.HostedServices;
using Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox
{
    internal static class OutboxTestSupport
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        internal static readonly DateTimeOffset BaseUtc = new(
            year: 2048,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            offset: TimeSpan.Zero);

        internal static HostedServicesTestSupport.MutableTimeProvider CreateTimeProvider(DateTimeOffset utcNow)
        {
            return new HostedServicesTestSupport.MutableTimeProvider(utcNow);
        }

        internal static SimulationCoreDbContext CreateDbContext(string databaseName)
        {
            return SimulationInfrastructureTestSupport.CreateDbContext(databaseName);
        }

        internal static T DeserializePayload<T>(OutboxMessage message)
            where T : notnull
        {
            T? payload = JsonSerializer.Deserialize<T>(
                json: message.PayloadJson,
                options: JsonOptions);
            return Assert.IsType<T>(payload);
        }

        internal static CityEnvironment CreateEnvironment(
            ClimateZone climateZone = ClimateZone.Temperate,
            Hemisphere hemisphere = Hemisphere.Northern,
            int utcOffsetMinutes = 180)
        {
            return CityEnvironment.Create(
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffset: CityUtcOffset.FromMinutes(utcOffsetMinutes));
        }

        internal static CityGenerationProfile CreateGenerationProfile()
        {
            return CityGenerationProfile.Create(
                sizeTier: CitySizeTier.Medium,
                urbanDensity: UrbanDensity.Balanced,
                developmentLevel: CityDevelopmentLevel.Balanced,
                economyProfile: CityEconomyProfile.Balanced,
                populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                plannedPeopleCount: 25_000);
        }

        internal static CityCreatedDomainEvent CreateCityCreatedDomainEvent(
            Guid? cityId = null,
            CityEnvironment? environment = null,
            CityGenerationProfile? generationProfile = null,
            DateTimeOffset? createdAtUtc = null)
        {
            return new CityCreatedDomainEvent(
                CityId: new CityId(cityId ?? Guid.Parse("11111111-1111-1111-1111-111111111111")),
                Name: new CityName("Outbox City"),
                Environment: environment ?? CreateEnvironment(),
                GenerationSeed: new CityGenerationSeed("outbox-seed"),
                RunId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ScenarioModelSetVersion: new ScenarioModelSetVersion("classic-city-v3"),
                GenerationProfile: generationProfile ?? CreateGenerationProfile(),
                PopulationBootstrapOperationId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CreatedAtUtc: createdAtUtc ?? BaseUtc);
        }

        internal static CityEnvironmentChangedDomainEvent CreateCityEnvironmentChangedDomainEvent(Guid? cityId = null)
        {
            return new CityEnvironmentChangedDomainEvent(
                CityId: new CityId(cityId ?? Guid.Parse("44444444-4444-4444-4444-444444444444")),
                From: CreateEnvironment(
                    climateZone: ClimateZone.Continental,
                    hemisphere: Hemisphere.Northern,
                    utcOffsetMinutes: 120),
                To: CreateEnvironment(
                    climateZone: ClimateZone.Arid,
                    hemisphere: Hemisphere.Southern,
                    utcOffsetMinutes: 240));
        }

        internal static WeatherState CreateWeatherState(
            SimTime? startedAt = null,
            SimTime? expectedUntil = null,
            WeatherType type = WeatherType.Clear,
            PrecipitationKind precipitationKind = PrecipitationKind.None,
            WeatherSeverity severity = WeatherSeverity.Calm)
        {
            return RepositoryTestData.CreateWeatherState(
                startedAt: startedAt ?? SimTime.FromUtc(BaseUtc.AddHours(1)),
                expectedUntil: expectedUntil ?? SimTime.FromUtc(BaseUtc.AddHours(4)),
                type: type,
                precipitationKind: precipitationKind,
                severity: severity);
        }

        internal static WeatherClimateProfile CreateClimateProfile()
        {
            return RepositoryTestData.CreateClimateProfile();
        }
    }
}
