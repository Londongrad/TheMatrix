using Matrix.SimulationSystems.Infrastructure.Persistence.Repositories;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityEnvironmentalConditionRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsStateAndGetBySimulationHostIdReturnsIt()
    {
        await using var dbContext = CreateDbContext();
        CityEnvironmentalConditionRepository repository = new(dbContext);
        var state = CreateState();

        await repository.AddAsync(state, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetBySimulationHostIdAsync(CreateHostId(), CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(CreateHostId(), loaded!.SimulationHostId);
        Assert.Equal(state.LastAppliedTickId, loaded.LastAppliedTickId);
        Assert.Equal(state.UtilityContinuityIndex, loaded.UtilityContinuityIndex);
    }

    [Fact]
    public async Task GetBySimulationHostIdAsync_ReturnsNullWhenStateDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        CityEnvironmentalConditionRepository repository = new(dbContext);

        var loaded = await repository.GetBySimulationHostIdAsync(CreateHostId(), CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetFreshBySimulationHostIdAsync_ClearsTrackedChangesBeforeLoading()
    {
        const long persistedTickId = 4;

        await using var dbContext = CreateDbContext();
        CityEnvironmentalConditionRepository repository = new(dbContext);
        var state = CreateState(lastAppliedTickId: persistedTickId);

        await repository.AddAsync(state, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        state.MarkTickApplied(12);

        var loaded = await repository.GetFreshBySimulationHostIdAsync(CreateHostId(), CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(persistedTickId, loaded!.LastAppliedTickId);
    }

    [Fact]
    public async Task GetBySimulationHostIdNoTrackingAsync_ReturnsDetachedEntity()
    {
        await using var dbContext = CreateDbContext();
        CityEnvironmentalConditionRepository repository = new(dbContext);
        var state = CreateState();

        await repository.AddAsync(state, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetBySimulationHostIdNoTrackingAsync(CreateHostId(), CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(EntityState.Detached, dbContext.Entry(loaded!).State);
    }
}
