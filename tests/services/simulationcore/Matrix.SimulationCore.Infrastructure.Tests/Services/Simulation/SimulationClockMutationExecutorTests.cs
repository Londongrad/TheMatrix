using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;

public sealed class SimulationClockMutationExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSimulationHostIsMissing_ReturnsFalse()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ExecuteAsync_WhenSimulationHostIsMissing_ReturnsFalse));
        var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
        SimulationId simulationId = new(Guid.NewGuid());
        var executor = CreateExecutor(dbContext, hostRepository);

        bool result = await executor.ExecuteAsync(
            simulationId,
            clock => clock.Pause(),
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal([simulationId], hostRepository.RequestedSimulationIds);
        Assert.Empty(dbContext.SimulationClocks);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSimulationHostIsArchived_ThrowsConflict()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ExecuteAsync_WhenSimulationHostIsArchived_ThrowsConflict));
        SimulationId simulationId = new(Guid.NewGuid());
        var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
        hostRepository.HostsBySimulationId[simulationId.Value] = SimulationInfrastructureTestSupport.CreateHost(
            simulationId,
            SimulationHostState.Archived);
        var executor = CreateExecutor(dbContext, hostRepository);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
            executor.ExecuteAsync(
                simulationId,
                clock => clock.Pause(),
                CancellationToken.None));

        Assert.Equal("SimulationCore.Simulation.ArchivedHost", exception.Code);
        Assert.Equal([simulationId], hostRepository.RequestedSimulationIds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClockIsMissing_ReturnsFalse()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ExecuteAsync_WhenClockIsMissing_ReturnsFalse));
        SimulationId simulationId = new(Guid.NewGuid());
        var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
        hostRepository.HostsBySimulationId[simulationId.Value] = SimulationInfrastructureTestSupport.CreateHost(simulationId);
        var executor = CreateExecutor(dbContext, hostRepository);

        bool result = await executor.ExecuteAsync(
            simulationId,
            clock => clock.Pause(),
            CancellationToken.None);

        Assert.False(result);
        Assert.Equal([simulationId], hostRepository.RequestedSimulationIds);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClockExists_PersistsMutation()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ExecuteAsync_WhenClockExists_PersistsMutation));
        DateTimeOffset createdAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);
        City city = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc);
        SimulationClock clock = SimulationInfrastructureTestSupport.CreateClock(
            city.Id,
            createdAtUtc.AddMinutes(30));
        await dbContext.Cities.AddAsync(city);
        await dbContext.SimulationClocks.AddAsync(clock);
        await dbContext.SaveChangesAsync();

        var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
        hostRepository.HostsBySimulationId[clock.SimulationId.Value] =
            SimulationInfrastructureTestSupport.CreateHost(clock.SimulationId);
        var executor = CreateExecutor(dbContext, hostRepository);

        bool result = await executor.ExecuteAsync(
            clock.SimulationId,
            currentClock => currentClock.Pause(),
            CancellationToken.None);

        SimulationClock persistedClock = await dbContext.SimulationClocks
           .AsNoTracking()
           .SingleAsync(x => x.Id == city.Id);

        Assert.True(result);
        Assert.Equal(ClockState.Paused, persistedClock.State);
        Assert.True(persistedClock.IsPaused);
        Assert.Equal(1L, persistedClock.TickId.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WhenArchivedHostIsAllowed_PersistsMutation()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(ExecuteAsync_WhenArchivedHostIsAllowed_PersistsMutation));
        DateTimeOffset createdAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);
        City city = SimulationInfrastructureTestSupport.CreateCity(createdAtUtc);
        SimulationClock clock = SimulationInfrastructureTestSupport.CreateClock(
            city.Id,
            createdAtUtc.AddMinutes(45));
        await dbContext.Cities.AddAsync(city);
        await dbContext.SimulationClocks.AddAsync(clock);
        await dbContext.SaveChangesAsync();

        var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
        hostRepository.HostsBySimulationId[clock.SimulationId.Value] =
            SimulationInfrastructureTestSupport.CreateHost(
                clock.SimulationId,
                SimulationHostState.Archived);
        var executor = CreateExecutor(dbContext, hostRepository);

        bool result = await executor.ExecuteAsync(
            clock.SimulationId,
            currentClock => currentClock.SetSpeed(SimSpeed.From(12m)),
            CancellationToken.None,
            allowArchivedHost: true);

        SimulationClock persistedClock = await dbContext.SimulationClocks
           .AsNoTracking()
           .SingleAsync(x => x.Id == city.Id);

        Assert.True(result);
        Assert.Equal(12m, persistedClock.Speed.Multiplier);
        Assert.Equal(1L, persistedClock.TickId.Value);
    }

    private static SimulationClockMutationExecutor CreateExecutor(
        SimulationCoreDbContext dbContext,
        SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository hostRepository)
    {
        return new SimulationClockMutationExecutor(
            dbContext,
            hostRepository,
            new SimulationOperationGate(),
            NullLogger<SimulationClockMutationExecutor>.Instance);
    }
}
