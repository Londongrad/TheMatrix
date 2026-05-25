using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityStockpileRepositoryTests
    {
        [Fact]
        public async Task AddAsync_PersistsStateAndGetBySimulationHostIdReturnsIt()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var repository = new CityStockpileRepository(dbContext);
            CityStockpileState state = CreateState();

            await repository.AddAsync(
                state: state,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            CityStockpileState? loaded = await repository.GetBySimulationHostIdAsync(
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
                expected: state.SupplyStressIndex,
                actual: loaded.SupplyStressIndex);
        }

        [Fact]
        public async Task GetBySimulationHostIdAsync_ReturnsNullWhenStateDoesNotExist()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var repository = new CityStockpileRepository(dbContext);

            CityStockpileState? loaded = await repository.GetBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.Null(loaded);
        }

        [Fact]
        public async Task DeleteBySimulationHostIdAsync_RemovesExistingStateAndIgnoresMissingState()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var repository = new CityStockpileRepository(dbContext);
            await repository.AddAsync(
                state: CreateState(),
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            await repository.DeleteBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            await repository.DeleteBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.Null(
                await repository.GetBySimulationHostIdAsync(
                    simulationHostId: CreateHostId(),
                    cancellationToken: CancellationToken.None));
        }
    }
}
