using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Matrix.SimulationSystems.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityEnvironmentalConditionRepositoryTests
    {
        [Fact]
        public async Task AddAsync_PersistsStateAndGetBySimulationHostIdReturnsIt()
        {
            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            CityEnvironmentalConditionRepository repository = new(dbContext);
            CityEnvironmentalConditionState state = CreateState();

            await repository.AddAsync(
                state: state,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            CityEnvironmentalConditionState? loaded = await repository.GetBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(
                expected: CreateHostId(),
                actual: loaded!.SimulationHostId);
            Assert.Equal(
                expected: state.LastAppliedTickId,
                actual: loaded.LastAppliedTickId);
            Assert.Equal(
                expected: state.UtilityContinuityIndex,
                actual: loaded.UtilityContinuityIndex);
        }

        [Fact]
        public async Task GetBySimulationHostIdAsync_ReturnsNullWhenStateDoesNotExist()
        {
            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            CityEnvironmentalConditionRepository repository = new(dbContext);

            CityEnvironmentalConditionState? loaded = await repository.GetBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.Null(loaded);
        }

        [Fact]
        public async Task GetFreshBySimulationHostIdAsync_ClearsTrackedChangesBeforeLoading()
        {
            const long persistedTickId = 4;

            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            CityEnvironmentalConditionRepository repository = new(dbContext);
            CityEnvironmentalConditionState state = CreateState(lastAppliedTickId: persistedTickId);

            await repository.AddAsync(
                state: state,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            state.MarkTickApplied(12);

            CityEnvironmentalConditionState? loaded = await repository.GetFreshBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(
                expected: persistedTickId,
                actual: loaded!.LastAppliedTickId);
        }

        [Fact]
        public async Task GetBySimulationHostIdNoTrackingAsync_ReturnsDetachedEntity()
        {
            await using SimulationSystemsDbContext dbContext = CreateDbContext();
            CityEnvironmentalConditionRepository repository = new(dbContext);
            CityEnvironmentalConditionState state = CreateState();

            await repository.AddAsync(
                state: state,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            CityEnvironmentalConditionState? loaded = await repository.GetBySimulationHostIdNoTrackingAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(
                expected: EntityState.Detached,
                actual: dbContext.Entry(loaded!)
                   .State);
        }
    }
}
