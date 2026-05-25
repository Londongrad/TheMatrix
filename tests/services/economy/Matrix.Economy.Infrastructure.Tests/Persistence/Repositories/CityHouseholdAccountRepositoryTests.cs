using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityHouseholdAccountRepositoryTests
    {
        [Fact]
        public async Task GetByExternalReferenceCode_ReturnsMatchingAccount()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount account = CreateHouseholdAccount(
                cityId: cityId,
                name: "Anderson",
                externalReferenceCode: "hh-anderson");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityHouseholdAccounts.AddRange(
                account,
                CreateHouseholdAccount(
                    cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    name: "Other",
                    externalReferenceCode: "hh-other"));
            await dbContext.SaveChangesAsync();

            CityHouseholdAccountRepository repository = new(dbContext);

            CityHouseholdAccount? result = await repository.GetByCityAndExternalReferenceCodeAsync(
                cityId: cityId,
                externalReferenceCode: "hh-anderson");

            Assert.Equal(
                expected: account.Id,
                actual: result?.Id);
        }

        [Fact]
        public async Task ListByCityAsync_FiltersAndOrdersByName()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityHouseholdAccounts.AddRange(
                CreateHouseholdAccount(
                    cityId: cityId,
                    name: "Zimmer",
                    externalReferenceCode: "hh-zimmer"),
                CreateHouseholdAccount(
                    cityId: cityId,
                    name: "Anderson",
                    externalReferenceCode: "hh-anderson"),
                CreateHouseholdAccount(
                    cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    name: "Other",
                    externalReferenceCode: "hh-other"));
            await dbContext.SaveChangesAsync();

            CityHouseholdAccountRepository repository = new(dbContext);

            IReadOnlyList<CityHouseholdAccount> accounts = await repository.ListByCityAsync(cityId);

            Assert.Equal(
                expected: 2,
                actual: accounts.Count);
            Assert.Collection(
                collection: accounts,
                x => Assert.Equal(
                    expected: "Anderson",
                    actual: x.Name),
                x => Assert.Equal(
                    expected: "Zimmer",
                    actual: x.Name));
        }
    }
}
