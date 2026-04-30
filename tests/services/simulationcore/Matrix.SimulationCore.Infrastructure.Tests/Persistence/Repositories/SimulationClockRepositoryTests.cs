using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;

public sealed class SimulationClockRepositoryTests
{
    [Fact]
    public async Task GetBySimulationIdAsync_WhenClockExists_ReturnsMatchingClock()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(GetBySimulationIdAsync_WhenClockExists_ReturnsMatchingClock));
        DateTimeOffset createdAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);
        City city = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc);
        SimulationClock clock = SimulationInfrastructureTestSupport.CreateClock(
            city.Id,
            createdAtUtc.AddMinutes(30));
        await dbContext.Cities.AddAsync(city);
        await dbContext.SimulationClocks.AddAsync(clock);
        await dbContext.SaveChangesAsync();
        var repository = new SimulationClockRepository(dbContext);

        SimulationClock? result = await repository.GetBySimulationIdAsync(clock.SimulationId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(city.Id, result.Id);
        Assert.Equal(clock.CurrentTime, result.CurrentTime);
        Assert.Equal(clock.Speed, result.Speed);
    }

    [Fact]
    public async Task ListActiveRunningSimulationIdsAsync_ReturnsOnlyRunningClocksForNonArchivedCities()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ListActiveRunningSimulationIdsAsync_ReturnsOnlyRunningClocksForNonArchivedCities));
        DateTimeOffset createdAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);

        City activeRunningCity = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc, name: "Active Running");
        SimulationClock activeRunningClock = SimulationInfrastructureTestSupport.CreateClock(
            activeRunningCity.Id,
            createdAtUtc.AddMinutes(10));

        City activePausedCity = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc.AddMinutes(1), name: "Active Paused");
        SimulationClock activePausedClock = SimulationInfrastructureTestSupport.CreateClock(
            activePausedCity.Id,
            createdAtUtc.AddMinutes(11));
        activePausedClock.Pause();

        City archivedRunningCity = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc.AddMinutes(2), name: "Archived Running");
        archivedRunningCity.Archive(createdAtUtc.AddHours(1));
        SimulationClock archivedRunningClock = SimulationInfrastructureTestSupport.CreateClock(
            archivedRunningCity.Id,
            createdAtUtc.AddMinutes(12));

        await dbContext.Cities.AddRangeAsync(activeRunningCity, activePausedCity, archivedRunningCity);
        await dbContext.SimulationClocks.AddRangeAsync(activeRunningClock, activePausedClock, archivedRunningClock);
        await dbContext.SaveChangesAsync();
        var repository = new SimulationClockRepository(dbContext);

        IReadOnlyList<SimulationId> result = await repository.ListActiveRunningSimulationIdsAsync(CancellationToken.None);

        Assert.Equal([activeRunningClock.SimulationId], result);
    }

    [Fact]
    public async Task DeleteBySimulationIdAsync_WhenClockExists_RemovesClockAndIgnoresMissingOne()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(DeleteBySimulationIdAsync_WhenClockExists_RemovesClockAndIgnoresMissingOne));
        DateTimeOffset createdAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);
        City city = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc);
        SimulationClock clock = SimulationInfrastructureTestSupport.CreateClock(
            city.Id,
            createdAtUtc.AddMinutes(40));
        await dbContext.Cities.AddAsync(city);
        await dbContext.SimulationClocks.AddAsync(clock);
        await dbContext.SaveChangesAsync();
        var repository = new SimulationClockRepository(dbContext);

        await repository.DeleteBySimulationIdAsync(clock.SimulationId, CancellationToken.None);
        await repository.DeleteBySimulationIdAsync(new SimulationId(Guid.NewGuid()), CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.Empty(await dbContext.SimulationClocks.AsNoTracking().ToListAsync());
    }
}
