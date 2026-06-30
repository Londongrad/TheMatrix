using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityPopulationPendingWeatherImpactRepositoryTests
    {
        [Fact]
        public async Task ListByCityAsync_OrdersTransitionsAndRemoveRangeKeepsLaterInsert()
        {
            await using PopulationTestDatabase database = PopulationInfrastructureTestSupport.CreateDbContext();
            var repository = new CityPopulationPendingWeatherImpactRepository(database.DbContext);
            var cityId = CityId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            CityPopulationPendingWeatherImpact later = CreateImpact(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                cityId,
                new DateTimeOffset(2048, 7, 10, 14, 0, 0, TimeSpan.Zero));
            CityPopulationPendingWeatherImpact earlier = CreateImpact(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                cityId,
                new DateTimeOffset(2048, 7, 10, 12, 0, 0, TimeSpan.Zero));
            await repository.AddAsync(later);
            await repository.AddAsync(earlier);
            await database.DbContext.SaveChangesAsync();

            IReadOnlyList<CityPopulationPendingWeatherImpact> loaded =
                await repository.ListByCityAsync(cityId);

            Assert.Equal([earlier.ImpactId, later.ImpactId], loaded.Select(impact => impact.ImpactId));
            Assert.Equal(earlier.PreviousWeather, loaded[0].PreviousWeather);
            Assert.Equal(earlier.CurrentWeather, loaded[0].CurrentWeather);
            Assert.Equal(PopulationClimateZone.Temperate, loaded[0].Environment!.ClimateZone);

            CityPopulationPendingWeatherImpact concurrent = CreateImpact(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                cityId,
                new DateTimeOffset(2048, 7, 10, 16, 0, 0, TimeSpan.Zero));
            await repository.AddAsync(concurrent);
            repository.RemoveRange(loaded);
            await database.DbContext.SaveChangesAsync();

            CityPopulationPendingWeatherImpact remaining = Assert.Single(
                await database.DbContext.CityPopulationPendingWeatherImpacts.ToArrayAsync());
            Assert.Equal(concurrent.ImpactId, remaining.ImpactId);
        }

        [Fact]
        public async Task DeleteByCityAsync_RemovesOnlyRequestedCity()
        {
            await using PopulationTestDatabase database = PopulationInfrastructureTestSupport.CreateDbContext();
            var repository = new CityPopulationPendingWeatherImpactRepository(database.DbContext);
            var deletedCityId = CityId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var retainedCityId = CityId.From(Guid.Parse("99999999-9999-9999-9999-999999999999"));
            await repository.AddAsync(
                CreateImpact(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    deletedCityId,
                    new DateTimeOffset(2048, 7, 10, 12, 0, 0, TimeSpan.Zero)));
            await repository.AddAsync(
                CreateImpact(
                    Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    retainedCityId,
                    new DateTimeOffset(2048, 7, 10, 12, 0, 0, TimeSpan.Zero)));
            await database.DbContext.SaveChangesAsync();

            await repository.DeleteByCityAsync(deletedCityId);

            CityPopulationPendingWeatherImpact remaining = Assert.Single(
                await database.DbContext.CityPopulationPendingWeatherImpacts.AsNoTracking().ToArrayAsync());
            Assert.Equal(retainedCityId, remaining.CityId);
        }

        private static CityPopulationPendingWeatherImpact CreateImpact(
            Guid impactId,
            CityId cityId,
            DateTimeOffset occurredAtUtc)
        {
            return CityPopulationPendingWeatherImpact.Create(
                impactId: impactId,
                cityId: cityId,
                currentDate: DateOnly.FromDateTime(occurredAtUtc.UtcDateTime),
                previousWeather: new WeatherImpactProfile(
                    Type: PopulationWeatherType.Clear,
                    Severity: PopulationWeatherSeverity.Calm,
                    PrecipitationKind: PopulationPrecipitationKind.None,
                    TemperatureC: 22m,
                    HumidityPercent: 45m,
                    WindSpeedKph: 12m,
                    CloudCoveragePercent: 35m,
                    PressureHpa: 1012m),
                currentWeather: new WeatherImpactProfile(
                    Type: PopulationWeatherType.Heatwave,
                    Severity: PopulationWeatherSeverity.Extreme,
                    PrecipitationKind: PopulationPrecipitationKind.None,
                    TemperatureC: 39m,
                    HumidityPercent: 45m,
                    WindSpeedKph: 12m,
                    CloudCoveragePercent: 35m,
                    PressureHpa: 1012m),
                environment: CityPopulationEnvironment.Create(
                    cityId: cityId,
                    climateZone: PopulationClimateZone.Temperate,
                    hemisphere: PopulationHemisphere.Northern,
                    utcOffsetMinutes: 180,
                    createdAtUtc: occurredAtUtc),
                occurredAtUtc: occurredAtUtc);
        }
    }
}
