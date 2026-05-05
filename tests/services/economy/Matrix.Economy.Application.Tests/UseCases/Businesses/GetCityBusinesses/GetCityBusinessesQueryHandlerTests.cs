using Matrix.Economy.Application.UseCases.Businesses.GetCityBusinesses;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.GetCityBusinesses;

public sealed class GetCityBusinessesQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsBusinessesToDtos()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var bakery = CreateBusiness(cityId, "Bakery", CityBusinessKind.RetailStore, 250m);
        bakery.RecordRetailSale(
            grossAmount: Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(40m),
            salesTaxAmount: Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(5m));
        var utility = CreateBusiness(cityId, "Utility", CityBusinessKind.Utility, 400m);
        var repository = new FakeCityBusinessRepository
        {
            Businesses =
            [
                bakery,
                utility,
                CreateBusiness(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Other", CityBusinessKind.Service, 100m)
            ]
        };
        var handler = new GetCityBusinessesQueryHandler(repository);

        IReadOnlyList<Matrix.Economy.Application.UseCases.Businesses.CityBusinessDto> result =
            await handler.Handle(new GetCityBusinessesQuery(cityId), CancellationToken.None);

        Assert.Equal(cityId, repository.RequestedCityId);
        Assert.Equal(2, result.Count);
        Assert.Equal("Bakery", result[0].Name);
        Assert.Equal("RetailStore", result[0].Kind);
        Assert.Equal(290m, result[0].Balance);
        Assert.Equal(5m, result[0].TaxReserve);
        Assert.Equal("Utility", result[1].Name);
    }
}
