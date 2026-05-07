using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityEconomyProgressionStateRepositoryTests
{
    [Fact]
    public async Task GetByCityAsync_ReturnsStoredState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityEconomyProgressionStates.Add(CreateProgressionState(cityId));
        await dbContext.SaveChangesAsync();

        CityEconomyProgressionStateRepository repository = new(dbContext);

        var state = await repository.GetByCityAsync(cityId);

        Assert.NotNull(state);
        Assert.Equal(cityId, state.CityId);
        Assert.Equal(12, state.LastCompletedTickId);
    }

    [Fact]
    public async Task AddAsync_PersistsState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        CityEconomyProgressionStateRepository repository = new(dbContext);

        await repository.AddAsync(CreateProgressionState(cityId));
        await dbContext.SaveChangesAsync();

        Assert.Single(dbContext.CityEconomyProgressionStates);
    }
}
