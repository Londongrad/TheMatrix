using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBudgetRepositoryTests
    {
        [Fact]
        public async Task GetByCityAsync_ReturnsMatchingBudget()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            CityBudgetRepository repository = new(dbContext);
            repository.Add(CreateBudget(cityId));
            repository.Add(CreateBudget(Guid.Parse("11111111-2222-3333-4444-555555555555")));
            await dbContext.SaveChangesAsync();

            CityBudget? budget = await repository.GetByCityAsync(cityId);

            Assert.NotNull(budget);
            Assert.Equal(
                expected: cityId,
                actual: budget.CityId);
        }

        [Fact]
        public async Task ListAsync_ReturnsAllBudgets()
        {
            await using EconomyDbContext dbContext = CreateDbContext();
            CityBudgetRepository repository = new(dbContext);
            repository.Add(CreateBudget(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")));
            repository.Add(CreateBudget(Guid.Parse("11111111-2222-3333-4444-555555555555")));
            await dbContext.SaveChangesAsync();

            IReadOnlyList<CityBudget> budgets = await repository.ListAsync();

            Assert.Equal(
                expected: 2,
                actual: budgets.Count);
        }
    }
}
