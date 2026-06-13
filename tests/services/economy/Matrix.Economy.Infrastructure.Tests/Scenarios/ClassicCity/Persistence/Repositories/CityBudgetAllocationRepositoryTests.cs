using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBudgetAllocationRepositoryTests
    {
        [Fact]
        public async Task GetByCityAndCategoryAsync_ReturnsMatchingAllocation()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBudgetAllocations.AddRange(
                CreateBudgetAllocation(
                    cityId: cityId,
                    category: CityBudgetCategory.Operations,
                    targetAmount: 100m),
                CreateBudgetAllocation(
                    cityId: cityId,
                    category: CityBudgetCategory.Infrastructure,
                    targetAmount: 150m),
                CreateBudgetAllocation(
                    cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    category: CityBudgetCategory.Operations,
                    targetAmount: 200m));
            await dbContext.SaveChangesAsync();

            CityBudgetAllocationRepository repository = new(dbContext);

            CityBudgetAllocation? allocation = await repository.GetByCityAndCategoryAsync(
                cityId: cityId,
                category: CityBudgetCategory.Infrastructure);

            Assert.NotNull(allocation);
            Assert.Equal(
                expected: CityBudgetCategory.Infrastructure,
                actual: allocation.Category);
            Assert.Equal(
                expected: cityId,
                actual: allocation.CityId);
        }

        [Fact]
        public async Task ListByCityAsync_FiltersAndOrdersByCategory()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBudgetAllocations.AddRange(
                CreateBudgetAllocation(
                    cityId: cityId,
                    category: CityBudgetCategory.Operations,
                    targetAmount: 100m),
                CreateBudgetAllocation(
                    cityId: cityId,
                    category: CityBudgetCategory.General,
                    targetAmount: 80m),
                CreateBudgetAllocation(
                    cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    category: CityBudgetCategory.Infrastructure,
                    targetAmount: 200m));
            await dbContext.SaveChangesAsync();

            CityBudgetAllocationRepository repository = new(dbContext);

            IReadOnlyList<CityBudgetAllocation> allocations = await repository.ListByCityAsync(cityId);

            Assert.Equal(
                expected: 2,
                actual: allocations.Count);
            Assert.Collection(
                collection: allocations,
                x => Assert.Equal(
                    expected: CityBudgetCategory.General,
                    actual: x.Category),
                x => Assert.Equal(
                    expected: CityBudgetCategory.Operations,
                    actual: x.Category));
        }
    }
}
