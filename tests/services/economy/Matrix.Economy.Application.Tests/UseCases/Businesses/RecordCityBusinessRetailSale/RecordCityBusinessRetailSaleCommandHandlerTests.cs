using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessRetailSale;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RecordCityBusinessRetailSale;

public sealed class RecordCityBusinessRetailSaleCommandHandlerTests
{
    [Fact]
    public async Task Handle_RecordsRetailSaleWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Corner Store", CityBusinessKind.RetailStore, 180m);
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var ledgerRepository = new FakeCityBusinessLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 11, 20, 0, TimeSpan.Zero));
        var handler = new RecordCityBusinessRetailSaleCommandHandler(
            businessRepository,
            ledgerRepository,
            unitOfWork,
            timeProvider);
        var command = new RecordCityBusinessRetailSaleCommand(
            BusinessId: business.Id,
            GrossAmount: 50m,
            SalesTaxAmount: 5m,
            Title: "Morning Sales",
            Description: "Retail batch");

        CityBusinessLedgerEntryDto result = await handler.Handle(command, CancellationToken.None);

        var entry = Assert.Single(ledgerRepository.AddedEntries);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, entry.OccurredAtUtc);
        Assert.Equal("RetailSale", result.Kind);
        Assert.Equal("RetailSale", result.Source);
        Assert.Equal(50m, result.Amount);
        Assert.Equal(5m, result.TaxAmount);
        Assert.Equal(230m, business.Balance.Amount);
        Assert.Equal(5m, business.TaxReserve.Amount);
        Assert.Equal(50m, business.TotalRetailTurnover.Amount);
        Assert.Equal(45m, business.TotalNetSalesRevenue.Amount);
    }
}
