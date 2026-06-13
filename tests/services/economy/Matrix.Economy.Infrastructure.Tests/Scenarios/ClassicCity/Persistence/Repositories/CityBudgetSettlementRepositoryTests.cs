using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBudgetSettlementRepositoryTests
    {
        [Fact]
        public async Task ExistsAsync_ReturnsTrueForMatchingCityAndTick()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBudgetSettlements.Add(
                CreateBudgetSettlement(
                    cityId: cityId,
                    tickId: 42,
                    correlationId: "corr-42"));
            await dbContext.SaveChangesAsync();

            CityBudgetSettlementRepository repository = new(dbContext);

            bool exists = await repository.ExistsAsync(
                cityId: cityId,
                tickId: 42);

            Assert.True(exists);
        }

        [Fact]
        public async Task AddAsync_PersistsSettlement()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            CityBudgetSettlementRepository repository = new(dbContext);

            await repository.AddAsync(
                CreateBudgetSettlement(
                    cityId: cityId,
                    tickId: 43,
                    correlationId: "corr-43"));
            await dbContext.SaveChangesAsync();

            Assert.Single(dbContext.CityBudgetSettlements);
        }
    }
}
