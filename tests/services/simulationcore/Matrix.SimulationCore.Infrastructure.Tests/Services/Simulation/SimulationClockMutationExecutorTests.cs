using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation
{
    public sealed class SimulationClockMutationExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenSimulationHostIsMissing_ReturnsFalse()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ExecuteAsync_WhenSimulationHostIsMissing_ReturnsFalse));
            var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
            SimulationId simulationId = new(Guid.NewGuid());
            SimulationClockMutationExecutor executor = CreateExecutor(
                dbContext: dbContext,
                hostRepository: hostRepository);

            bool result = await executor.ExecuteAsync(
                simulationId: simulationId,
                mutate: clock => clock.Pause(),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
            Assert.Equal(
                expected: [simulationId],
                actual: hostRepository.RequestedSimulationIds);
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
                simulationId: simulationId,
                state: SimulationHostState.Archived);
            SimulationClockMutationExecutor executor = CreateExecutor(
                dbContext: dbContext,
                hostRepository: hostRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                executor.ExecuteAsync(
                    simulationId: simulationId,
                    mutate: clock => clock.Pause(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "SimulationCore.Simulation.ArchivedHost",
                actual: exception.Code);
            Assert.Equal(
                expected: [simulationId],
                actual: hostRepository.RequestedSimulationIds);
        }

        [Fact]
        public async Task ExecuteAsync_WhenClockIsMissing_ReturnsFalse()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ExecuteAsync_WhenClockIsMissing_ReturnsFalse));
            SimulationId simulationId = new(Guid.NewGuid());
            var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
            hostRepository.HostsBySimulationId[simulationId.Value] =
                SimulationInfrastructureTestSupport.CreateHost(simulationId);
            SimulationClockMutationExecutor executor = CreateExecutor(
                dbContext: dbContext,
                hostRepository: hostRepository);

            bool result = await executor.ExecuteAsync(
                simulationId: simulationId,
                mutate: clock => clock.Pause(),
                cancellationToken: CancellationToken.None);

            Assert.False(result);
            Assert.Equal(
                expected: [simulationId],
                actual: hostRepository.RequestedSimulationIds);
        }

        [Fact]
        public async Task ExecuteAsync_WhenClockExists_PersistsMutation()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ExecuteAsync_WhenClockExists_PersistsMutation));
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

            var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
            hostRepository.HostsBySimulationId[clock.SimulationId.Value] =
                SimulationInfrastructureTestSupport.CreateHost(clock.SimulationId);
            SimulationClockMutationExecutor executor = CreateExecutor(
                dbContext: dbContext,
                hostRepository: hostRepository);

            bool result = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                mutate: currentClock => currentClock.Pause(),
                cancellationToken: CancellationToken.None);

            SimulationClock persistedClock = await dbContext.SimulationClocks
               .AsNoTracking()
               .SingleAsync(x => x.Id == city.Id);

            Assert.True(result);
            Assert.Equal(
                expected: ClockState.Paused,
                actual: persistedClock.State);
            Assert.True(persistedClock.IsPaused);
            Assert.Equal(
                expected: 1L,
                actual: persistedClock.TickId.Value);
        }

        [Fact]
        public async Task ExecuteAsync_WhenArchivedHostIsAllowed_PersistsMutation()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ExecuteAsync_WhenArchivedHostIsAllowed_PersistsMutation));
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
                startAtUtc: createdAtUtc.AddMinutes(45));
            await dbContext.Cities.AddAsync(city);
            await dbContext.SimulationClocks.AddAsync(clock);
            await dbContext.SaveChangesAsync();

            var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
            hostRepository.HostsBySimulationId[clock.SimulationId.Value] =
                SimulationInfrastructureTestSupport.CreateHost(
                    simulationId: clock.SimulationId,
                    state: SimulationHostState.Archived);
            SimulationClockMutationExecutor executor = CreateExecutor(
                dbContext: dbContext,
                hostRepository: hostRepository);

            bool result = await executor.ExecuteAsync(
                simulationId: clock.SimulationId,
                mutate: currentClock => currentClock.SetSpeed(SimSpeed.From(12m)),
                cancellationToken: CancellationToken.None,
                allowArchivedHost: true);

            SimulationClock persistedClock = await dbContext.SimulationClocks
               .AsNoTracking()
               .SingleAsync(x => x.Id == city.Id);

            Assert.True(result);
            Assert.Equal(
                expected: 12m,
                actual: persistedClock.Speed.Multiplier);
            Assert.Equal(
                expected: 1L,
                actual: persistedClock.TickId.Value);
        }

        [Fact]
        public async Task ExecuteAsync_WhenConcurrencyConflictResolves_RetriesAndPersistsMutation()
        {
            string databaseName = nameof(ExecuteAsync_WhenConcurrencyConflictResolves_RetriesAndPersistsMutation);
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
                startAtUtc: createdAtUtc.AddMinutes(20));
            var interceptor = new SimulationInfrastructureTestSupport.ConcurrencySaveChangesInterceptor(0);

            using (SimulationCoreDbContext dbContext =
                   SimulationInfrastructureTestSupport.CreateDbContext(
                       databaseName: databaseName,
                       interceptor))
            {
                await dbContext.Cities.AddAsync(city);
                await dbContext.SimulationClocks.AddAsync(clock);
                await dbContext.SaveChangesAsync();

                interceptor.ArmFailures(2);
                int attemptsBeforeMutation = interceptor.SaveChangesAttemptCount;

                var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
                hostRepository.HostsBySimulationId[clock.SimulationId.Value] =
                    SimulationInfrastructureTestSupport.CreateHost(clock.SimulationId);
                SimulationClockMutationExecutor executor = CreateExecutor(
                    dbContext: dbContext,
                    hostRepository: hostRepository);

                bool result = await executor.ExecuteAsync(
                    simulationId: clock.SimulationId,
                    mutate: currentClock => currentClock.SetSpeed(SimSpeed.From(24m)),
                    cancellationToken: CancellationToken.None);

                SimulationClock persistedClock = await dbContext.SimulationClocks
                   .AsNoTracking()
                   .SingleAsync(x => x.Id == city.Id);

                Assert.True(result);
                Assert.Equal(
                    expected: 24m,
                    actual: persistedClock.Speed.Multiplier);
                Assert.Equal(
                    expected: 1L,
                    actual: persistedClock.TickId.Value);
                Assert.Equal(
                    expected: 3,
                    actual: interceptor.SaveChangesAttemptCount - attemptsBeforeMutation);
                Assert.Equal(
                    expected: 3,
                    actual: hostRepository.RequestedSimulationIds.Count);
            }
        }

        [Fact]
        public async Task ExecuteAsync_WhenConcurrencyConflictPersists_ThrowsClockConflict()
        {
            string databaseName = nameof(ExecuteAsync_WhenConcurrencyConflictPersists_ThrowsClockConflict);
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
                startAtUtc: createdAtUtc.AddMinutes(25));
            var interceptor = new SimulationInfrastructureTestSupport.ConcurrencySaveChangesInterceptor(0);

            using (SimulationCoreDbContext dbContext =
                   SimulationInfrastructureTestSupport.CreateDbContext(
                       databaseName: databaseName,
                       interceptor))
            {
                await dbContext.Cities.AddAsync(city);
                await dbContext.SimulationClocks.AddAsync(clock);
                await dbContext.SaveChangesAsync();

                interceptor.ArmFailures(3);
                int attemptsBeforeMutation = interceptor.SaveChangesAttemptCount;

                var hostRepository = new SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository();
                hostRepository.HostsBySimulationId[clock.SimulationId.Value] =
                    SimulationInfrastructureTestSupport.CreateHost(clock.SimulationId);
                SimulationClockMutationExecutor executor = CreateExecutor(
                    dbContext: dbContext,
                    hostRepository: hostRepository);

                MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() =>
                    executor.ExecuteAsync(
                        simulationId: clock.SimulationId,
                        mutate: currentClock => currentClock.Pause(),
                        cancellationToken: CancellationToken.None));

                SimulationClock persistedClock = await dbContext.SimulationClocks
                   .AsNoTracking()
                   .SingleAsync(x => x.Id == city.Id);

                Assert.Equal(
                    expected: "SimulationCore.SimulationClockConflict",
                    actual: exception.Code);
                Assert.Equal(
                    expected: ClockState.Running,
                    actual: persistedClock.State);
                Assert.Equal(
                    expected: 0L,
                    actual: persistedClock.TickId.Value);
                Assert.Equal(
                    expected: 3,
                    actual: interceptor.SaveChangesAttemptCount - attemptsBeforeMutation);
                Assert.Equal(
                    expected: 3,
                    actual: hostRepository.RequestedSimulationIds.Count);
            }
        }

        private static SimulationClockMutationExecutor CreateExecutor(
            SimulationCoreDbContext dbContext,
            SimulationInfrastructureTestSupport.FakeSimulationHostReadRepository hostRepository)
        {
            return new SimulationClockMutationExecutor(
                dbContext: dbContext,
                simulationHostRepository: hostRepository,
                operationGate: new SimulationOperationGate(),
                logger: NullLogger<SimulationClockMutationExecutor>.Instance);
        }
    }
}
