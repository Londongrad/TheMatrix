using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;

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

        CityWeather? result = await repository.GetByCityIdAsync(city.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(city.Id, result.CityId);
        Assert.Equal(cityWeather.CurrentState.Type, result.CurrentState.Type);
        Assert.Equal(cityWeather.CurrentState.ExpectedUntil, result.CurrentState.ExpectedUntil);
        Assert.Equal(cityWeather.ClimateProfile.ClimateZone, result.ClimateProfile.ClimateZone);
    }

    [Fact]
    public async Task AddAsync_WhenWeatherIsAdded_PersistsAggregate()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(AddAsync_WhenWeatherIsAdded_PersistsAggregate));
        City city = RepositoryTestData.CreateCity();
        CityWeather cityWeather = RepositoryTestData.CreateCityWeather(
            city.Id,
            currentState: RepositoryTestData.CreateWeatherState(
                startedAt: SimTime.FromUtc(RepositoryTestData.BaseUtc.AddHours(1)),
                expectedUntil: SimTime.FromUtc(RepositoryTestData.BaseUtc.AddHours(4)),
                type: WeatherType.Rain,
                precipitationKind: PrecipitationKind.Rain,
                severity: WeatherSeverity.Moderate));
        await dbContext.Cities.AddAsync(city);
        await dbContext.SaveChangesAsync();
        var repository = new CityWeatherRepository(dbContext);

        await repository.AddAsync(cityWeather, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        CityWeather persisted = await dbContext.CityWeathers
           .AsNoTracking()
           .SingleAsync(x => x.Id == city.Id);

        Assert.Equal(WeatherType.Rain, persisted.CurrentState.Type);
        Assert.Equal(PrecipitationKind.Rain, persisted.CurrentState.PrecipitationKind);
        Assert.Equal(WeatherSeverity.Moderate, persisted.CurrentState.Severity);
    }
}
