using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityEconomyProgressionStateRepositoryTests
    {
        [Fact]
        public async Task GetByCityAsync_ReturnsStoredState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityEconomyProgressionStates.Add(CreateProgressionState(cityId));
            await dbContext.SaveChangesAsync();

            CityEconomyProgressionStateRepository repository = new(dbContext);

            CityEconomyProgressionState? state = await repository.GetByCityAsync(cityId);

            Assert.NotNull(state);
            Assert.Equal(
                expected: cityId,
                actual: state.CityId);
            Assert.Equal(
                expected: 12,
                actual: state.LastCompletedTickId);
        }

        [Fact]
        public async Task AddAsync_PersistsState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            CityEconomyProgressionStateRepository repository = new(dbContext);

            await repository.AddAsync(CreateProgressionState(cityId));
            await dbContext.SaveChangesAsync();

            Assert.Single(dbContext.CityEconomyProgressionStates);
        }
    }
}
