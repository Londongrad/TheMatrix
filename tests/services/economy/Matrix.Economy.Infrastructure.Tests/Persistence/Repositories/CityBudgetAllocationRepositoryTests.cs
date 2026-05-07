using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityBudgetAllocationRepositoryTests
{
    [Fact]
    public async Task GetByCityAndCategoryAsync_ReturnsMatchingAllocation()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityBudgetAllocations.AddRange(
            CreateBudgetAllocation(cityId, CityBudgetCategory.Operations, 100m),
            CreateBudgetAllocation(cityId, CityBudgetCategory.Infrastructure, 150m),
            CreateBudgetAllocation(Guid.Parse("11111111-2222-3333-4444-555555555555"), CityBudgetCategory.Operations, 200m));
        await dbContext.SaveChangesAsync();

        CityBudgetAllocationRepository repository = new(dbContext);

        var allocation = await repository.GetByCityAndCategoryAsync(cityId, CityBudgetCategory.Infrastructure);

        Assert.NotNull(allocation);
        Assert.Equal(CityBudgetCategory.Infrastructure, allocation.Category);
        Assert.Equal(cityId, allocation.CityId);
    }

    [Fact]
    public async Task ListByCityAsync_FiltersAndOrdersByCategory()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityBudgetAllocations.AddRange(
            CreateBudgetAllocation(cityId, CityBudgetCategory.Operations, 100m),
            CreateBudgetAllocation(cityId, CityBudgetCategory.General, 80m),
            CreateBudgetAllocation(Guid.Parse("11111111-2222-3333-4444-555555555555"), CityBudgetCategory.Infrastructure, 200m));
        await dbContext.SaveChangesAsync();

        CityBudgetAllocationRepository repository = new(dbContext);

        var allocations = await repository.ListByCityAsync(cityId);

        Assert.Equal(2, allocations.Count);
        Assert.Collection(
            allocations,
            x => Assert.Equal(CityBudgetCategory.General, x.Category),
            x => Assert.Equal(CityBudgetCategory.Operations, x.Category));
    }
}
