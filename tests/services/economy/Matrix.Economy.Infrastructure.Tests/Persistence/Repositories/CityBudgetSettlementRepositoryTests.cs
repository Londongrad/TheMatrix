using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories;

public sealed class CityBudgetSettlementRepositoryTests
{
    [Fact]
    public async Task ExistsAsync_ReturnsTrueForMatchingCityAndTick()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        dbContext.CityBudgetSettlements.Add(CreateBudgetSettlement(cityId, 42, "corr-42"));
        await dbContext.SaveChangesAsync();

        CityBudgetSettlementRepository repository = new(dbContext);

        bool exists = await repository.ExistsAsync(cityId, 42);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddAsync_PersistsSettlement()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using var dbContext = CreateDbContext();
        CityBudgetSettlementRepository repository = new(dbContext);

        await repository.AddAsync(CreateBudgetSettlement(cityId, 43, "corr-43"));
        await dbContext.SaveChangesAsync();

        Assert.Single(dbContext.CityBudgetSettlements);
    }
}
