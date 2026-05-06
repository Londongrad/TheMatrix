using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessExpense;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RecordCityBusinessExpense;

public sealed class RecordCityBusinessExpenseCommandHandlerTests
{
    [Fact]
    public async Task Handle_RecordsExpenseWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Transit Depot", CityBusinessKind.Service, 300m);
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var ledgerRepository = new FakeCityBusinessLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 10, 45, 0, TimeSpan.Zero));
        var handler = new RecordCityBusinessExpenseCommandHandler(
            businessRepository,
            ledgerRepository,
            unitOfWork,
            timeProvider);
        var command = new RecordCityBusinessExpenseCommand(
            BusinessId: business.Id,
            Amount: 75m,
            Title: "Fuel Purchase",
            Description: "Diesel refill");

        CityBusinessLedgerEntryDto result = await handler.Handle(command, CancellationToken.None);

        var entry = Assert.Single(ledgerRepository.AddedEntries);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, entry.OccurredAtUtc);
        Assert.Equal("OperatingExpense", result.Kind);
        Assert.Equal("Operations", result.Source);
        Assert.Equal(75m, result.Amount);
        Assert.Equal(0m, result.TaxAmount);
        Assert.Equal(225m, business.Balance.Amount);
        Assert.Equal(75m, business.TotalOperatingExpenses.Amount);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.OccurredAtUtc);
    }
}
