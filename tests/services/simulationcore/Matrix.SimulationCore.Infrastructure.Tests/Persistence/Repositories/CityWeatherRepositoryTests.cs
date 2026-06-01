using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityWeatherRepositoryTests
    {
        [Fact]
        public async Task GetByCityIdAsync_WhenWeatherExists_ReturnsMatchingAggregate()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(GetByCityIdAsync_WhenWeatherExists_ReturnsMatchingAggregate));
            City city = RepositoryTestData.CreateCity();
            CityWeather cityWeather = RepositoryTestData.CreateCityWeather(city.Id);
            await dbContext.Cities.AddAsync(city);
            await dbContext.CityWeathers.AddAsync(cityWeather);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            var repository = new CityWeatherRepository(dbContext);

            CityWeather? result = await repository.GetByCityIdAsync(
                cityId: city.Id,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: city.Id,
                actual: result.CityId);
            Assert.Equal(
                expected: cityWeather.CurrentState.Type,
                actual: result.CurrentState.Type);
            Assert.Equal(
                expected: cityWeather.CurrentState.ExpectedUntil,
                actual: result.CurrentState.ExpectedUntil);
            Assert.Equal(
                expected: cityWeather.ClimateProfile.ClimateZone,
                actual: result.ClimateProfile.ClimateZone);
        }

        [Fact]
        public async Task AddAsync_WhenWeatherIsAdded_PersistsAggregate()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(AddAsync_WhenWeatherIsAdded_PersistsAggregate));
            City city = RepositoryTestData.CreateCity();
            CityWeather cityWeather = RepositoryTestData.CreateCityWeather(
                cityId: city.Id,
                currentState: RepositoryTestData.CreateWeatherState(
                    startedAt: SimTime.FromUtc(RepositoryTestData.BaseUtc.AddHours(1)),
                    expectedUntil: SimTime.FromUtc(RepositoryTestData.BaseUtc.AddHours(4)),
                    type: WeatherType.Rain,
                    precipitationKind: PrecipitationKind.Rain,
                    severity: WeatherSeverity.Moderate));
            await dbContext.Cities.AddAsync(city);
            await dbContext.SaveChangesAsync();
            var repository = new CityWeatherRepository(dbContext);

            await repository.AddAsync(
                cityWeather: cityWeather,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            CityWeather persisted = await dbContext.CityWeathers
               .AsNoTracking()
               .SingleAsync(x => x.Id == city.Id);

            Assert.Equal(
                expected: WeatherType.Rain,
                actual: persisted.CurrentState.Type);
            Assert.Equal(
                expected: PrecipitationKind.Rain,
                actual: persisted.CurrentState.PrecipitationKind);
            Assert.Equal(
                expected: WeatherSeverity.Moderate,
                actual: persisted.CurrentState.Severity);
        }
    }
}
