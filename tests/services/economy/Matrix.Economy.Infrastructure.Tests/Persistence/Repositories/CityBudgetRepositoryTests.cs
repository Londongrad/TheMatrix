using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityBudgetRepositoryTests
{
    [Fact]
    public async Task GetByCityAsync_ReturnsMatchingBudget()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        CityBudgetRepository repository = new(dbContext);
        repository.Add(CreateBudget(cityId));
        repository.Add(CreateBudget(Guid.Parse("11111111-2222-3333-4444-555555555555")));
        await dbContext.SaveChangesAsync();

        var budget = await repository.GetByCityAsync(cityId);

        Assert.NotNull(budget);
        Assert.Equal(cityId, budget.CityId);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllBudgets()
    {
        await using var dbContext = CreateDbContext();
        CityBudgetRepository repository = new(dbContext);
        repository.Add(CreateBudget(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")));
        repository.Add(CreateBudget(Guid.Parse("11111111-2222-3333-4444-555555555555")));
        await dbContext.SaveChangesAsync();

        var budgets = await repository.ListAsync();

        Assert.Equal(2, budgets.Count);
    }
}
