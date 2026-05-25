using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class SimulationClockRepositoryTests
    {
        [Fact]
        public async Task GetBySimulationIdAsync_WhenClockExists_ReturnsMatchingClock()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(GetBySimulationIdAsync_WhenClockExists_ReturnsMatchingClock));
            DateTimeOffset createdAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);
            City city = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc);
            SimulationClock clock = SimulationInfrastructureTestSupport.CreateClock(
                cityId: city.Id,
                startAtUtc: createdAtUtc.AddMinutes(30));
            await dbContext.Cities.AddAsync(city);
            await dbContext.SimulationClocks.AddAsync(clock);
            await dbContext.SaveChangesAsync();
            var repository = new SimulationClockRepository(dbContext);

            SimulationClock? result = await repository.GetBySimulationIdAsync(
                simulationId: clock.SimulationId,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: city.Id,
                actual: result.Id);
            Assert.Equal(
                expected: clock.CurrentTime,
                actual: result.CurrentTime);
            Assert.Equal(
                expected: clock.Speed,
                actual: result.Speed);
        }

        [Fact]
        public async Task ListActiveRunningSimulationIdsAsync_ReturnsOnlyRunningClocksForNonArchivedCities()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ListActiveRunningSimulationIdsAsync_ReturnsOnlyRunningClocksForNonArchivedCities));
            DateTimeOffset createdAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);

            City activeRunningCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: createdAtUtc,
                name: "Active Running");
            SimulationClock activeRunningClock = SimulationInfrastructureTestSupport.CreateClock(
                cityId: activeRunningCity.Id,
                startAtUtc: createdAtUtc.AddMinutes(10));

            City activePausedCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: createdAtUtc.AddMinutes(1),
                name: "Active Paused");
            SimulationClock activePausedClock = SimulationInfrastructureTestSupport.CreateClock(
                cityId: activePausedCity.Id,
                startAtUtc: createdAtUtc.AddMinutes(11));
            activePausedClock.Pause();

            City archivedRunningCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: createdAtUtc.AddMinutes(2),
                name: "Archived Running");
            archivedRunningCity.Archive(createdAtUtc.AddHours(1));
            SimulationClock archivedRunningClock = SimulationInfrastructureTestSupport.CreateClock(
                cityId: archivedRunningCity.Id,
                startAtUtc: createdAtUtc.AddMinutes(12));

            await dbContext.Cities.AddRangeAsync(
                activeRunningCity,
                activePausedCity,
                archivedRunningCity);
            await dbContext.SimulationClocks.AddRangeAsync(
                activeRunningClock,
                activePausedClock,
                archivedRunningClock);
            await dbContext.SaveChangesAsync();
            var repository = new SimulationClockRepository(dbContext);

            IReadOnlyList<SimulationId> result =
                await repository.ListActiveRunningSimulationIdsAsync(CancellationToken.None);

            Assert.Equal(
                expected: [activeRunningClock.SimulationId],
                actual: result);
        }

        [Fact]
        public async Task DeleteBySimulationIdAsync_WhenClockExists_RemovesClockAndIgnoresMissingOne()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(DeleteBySimulationIdAsync_WhenClockExists_RemovesClockAndIgnoresMissingOne));
            DateTimeOffset createdAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);
            City city = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc);
            SimulationClock clock = SimulationInfrastructureTestSupport.CreateClock(
                cityId: city.Id,
                startAtUtc: createdAtUtc.AddMinutes(40));
            await dbContext.Cities.AddAsync(city);
            await dbContext.SimulationClocks.AddAsync(clock);
            await dbContext.SaveChangesAsync();
            var repository = new SimulationClockRepository(dbContext);

            await repository.DeleteBySimulationIdAsync(
                simulationId: clock.SimulationId,
                cancellationToken: CancellationToken.None);
            await repository.DeleteBySimulationIdAsync(
                simulationId: new SimulationId(Guid.NewGuid()),
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            Assert.Empty(
                await dbContext.SimulationClocks.AsNoTracking()
                   .ToListAsync());
        }
    }
}
