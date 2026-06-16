using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBusinessRepositoryTests
    {
        [Fact]
        public async Task GetByExternalReferenceAndTemplateKey_ReturnsMatchingBusiness()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness bakery = CreateBusiness(
                cityId: cityId,
                name: "Bakery",
                externalReferenceCode: "biz-bakery",
                templateKey: "tpl-bakery");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBusinesses.AddRange(
                bakery,
                CreateBusiness(
                    cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    name: "Other",
                    externalReferenceCode: "biz-other",
                    templateKey: "tpl-other"));
            await dbContext.SaveChangesAsync();

            CityBusinessRepository repository = new(dbContext);

            CityBusiness? byReference = await repository.GetByCityAndExternalReferenceCodeAsync(
                cityId: cityId,
                externalReferenceCode: "biz-bakery");
            CityBusiness? byTemplate = await repository.GetByCityAndTemplateKeyAsync(
                cityId: cityId,
                templateKey: "tpl-bakery");

            Assert.Equal(
                expected: bakery.Id,
                actual: byReference?.Id);
            Assert.Equal(
                expected: bakery.Id,
                actual: byTemplate?.Id);
        }

        [Fact]
        public async Task ListByCityAsync_FiltersAndOrdersByName()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBusinesses.AddRange(
                CreateBusiness(
                    cityId: cityId,
                    name: "Zoo",
                    externalReferenceCode: "biz-zoo",
                    templateKey: "tpl-zoo"),
                CreateBusiness(
                    cityId: cityId,
                    name: "Bakery",
                    externalReferenceCode: "biz-bakery",
                    templateKey: "tpl-bakery"),
                CreateBusiness(
                    cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    name: "Clinic",
                    externalReferenceCode: "biz-clinic",
                    templateKey: "tpl-clinic"));
            await dbContext.SaveChangesAsync();

            CityBusinessRepository repository = new(dbContext);

            IReadOnlyList<CityBusiness> businesses = await repository.ListByCityAsync(cityId);

            Assert.Equal(
                expected: 2,
                actual: businesses.Count);
            Assert.Collection(
                collection: businesses,
                x => Assert.Equal(
                    expected: "Bakery",
                    actual: x.Name),
                x => Assert.Equal(
                    expected: "Zoo",
                    actual: x.Name));
        }
    }
}
