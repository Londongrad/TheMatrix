using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityEconomyCostProfileStateRepositoryTests
{
    [Fact]
    public async Task GetByCityAsync_ReturnsStoredState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityEconomyCostProfileStates.Add(CreateCostProfileState(cityId));
        await dbContext.SaveChangesAsync();

        CityEconomyCostProfileStateRepository repository = new(dbContext);

        var state = await repository.GetByCityAsync(cityId);

        Assert.NotNull(state);
        Assert.Equal(cityId, state.CityId);
        Assert.Equal(1.25m, state.CostOfLivingIndex);
    }

    [Fact]
    public async Task AddAsync_PersistsState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        CityEconomyCostProfileStateRepository repository = new(dbContext);

        await repository.AddAsync(CreateCostProfileState(cityId));
        await dbContext.SaveChangesAsync();

        Assert.Single(dbContext.CityEconomyCostProfileStates);
    }
}
