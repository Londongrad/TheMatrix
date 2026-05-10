using Matrix.Resources.Infrastructure.Persistence.Repositories;
using Matrix.Resources.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityStockpileRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsStateAndGetBySimulationHostIdReturnsIt()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CityStockpileRepository(dbContext);
        var state = CreateState();

        await repository.AddAsync(state, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetBySimulationHostIdAsync(CreateHostId(), CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(CreateHostId(), loaded!.SimulationHostId);
        Assert.Equal(state.LastAppliedTickId, loaded.LastAppliedTickId);
        Assert.Equal(state.SupplyStressIndex, loaded.SupplyStressIndex);
    }

    [Fact]
    public async Task GetBySimulationHostIdAsync_ReturnsNullWhenStateDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var repository = new CityStockpileRepository(dbContext);

        var loaded = await repository.GetBySimulationHostIdAsync(CreateHostId(), CancellationToken.None);

        Assert.Null(loaded);
    }
}
