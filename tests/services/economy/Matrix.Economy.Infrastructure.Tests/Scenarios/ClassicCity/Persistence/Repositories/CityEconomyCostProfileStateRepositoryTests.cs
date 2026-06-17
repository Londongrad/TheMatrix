using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityEconomyCostProfileStateRepositoryTests
    {
        [Fact]
        public async Task GetByCityAsync_ReturnsStoredState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityEconomyCostProfileStates.Add(CreateCostProfileState(cityId));
            await dbContext.SaveChangesAsync();

            CityEconomyCostProfileStateRepository repository = new(dbContext);

            CityEconomyCostProfileState? state = await repository.GetByCityAsync(cityId);

            Assert.NotNull(state);
            Assert.Equal(
                expected: cityId,
                actual: state.CityId);
            Assert.Equal(
                expected: 1.25m,
                actual: state.CostOfLivingIndex);
        }

        [Fact]
        public async Task AddAsync_PersistsState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            CityEconomyCostProfileStateRepository repository = new(dbContext);

            await repository.AddAsync(CreateCostProfileState(cityId));
            await dbContext.SaveChangesAsync();

            Assert.Single(dbContext.CityEconomyCostProfileStates);
        }
    }
}
