using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinesses;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Businesses.GetCityBusinesses
{
    public sealed class GetCityBusinessesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsBusinessesToDtos()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness bakery = CreateBusiness(
                cityId: cityId,
                name: "Bakery",
                kind: CityBusinessKind.RetailStore,
                initialCapital: 250m);
            bakery.RecordRetailSale(
                grossAmount: Money.FromDecimal(40m),
                salesTaxAmount: Money.FromDecimal(5m));
            CityBusiness utility = CreateBusiness(
                cityId: cityId,
                name: "Utility",
                kind: CityBusinessKind.Utility,
                initialCapital: 400m);
            var repository = new FakeCityBusinessRepository
            {
                Businesses =
                [
                    bakery,
                    utility,
                    CreateBusiness(
                        cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        name: "Other",
                        kind: CityBusinessKind.Service,
                        initialCapital: 100m)
                ]
            };
            var handler = new GetCityBusinessesQueryHandler(repository);

            IReadOnlyList<CityBusinessDto> result =
                await handler.Handle(
                    request: new GetCityBusinessesQuery(cityId),
                    cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: repository.RequestedCityId);
            Assert.Equal(
                expected: 2,
                actual: result.Count);
            Assert.Equal(
                expected: "Bakery",
                actual: result[0].Name);
            Assert.Equal(
                expected: "RetailStore",
                actual: result[0].Kind);
            Assert.Equal(
                expected: 290m,
                actual: result[0].Balance);
            Assert.Equal(
                expected: 5m,
                actual: result[0].TaxReserve);
            Assert.Equal(
                expected: "Utility",
                actual: result[1].Name);
        }
    }
}
