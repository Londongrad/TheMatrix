using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessRetailSale;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessRetailSale
{
    public sealed class RecordCityBusinessRetailSaleCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RecordsRetailSaleWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Corner Store",
                kind: CityBusinessKind.RetailStore,
                initialCapital: 180m);
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var ledgerRepository = new FakeCityBusinessLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 11,
                    minute: 20,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RecordCityBusinessRetailSaleCommandHandler(
                businessRepository: businessRepository,
                ledgerRepository: ledgerRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RecordCityBusinessRetailSaleCommand(
                BusinessId: business.Id,
                GrossAmount: 50m,
                SalesTaxAmount: 5m,
                Title: "Morning Sales",
                Description: "Retail batch");

            CityBusinessLedgerEntryDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityBusinessLedgerEntry entry = Assert.Single(ledgerRepository.AddedEntries);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: entry.OccurredAtUtc);
            Assert.Equal(
                expected: "RetailSale",
                actual: result.Kind);
            Assert.Equal(
                expected: "RetailSale",
                actual: result.Source);
            Assert.Equal(
                expected: 50m,
                actual: result.Amount);
            Assert.Equal(
                expected: 5m,
                actual: result.TaxAmount);
            Assert.Equal(
                expected: 230m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 5m,
                actual: business.TaxReserve.Amount);
            Assert.Equal(
                expected: 50m,
                actual: business.TotalRetailTurnover.Amount);
            Assert.Equal(
                expected: 45m,
                actual: business.TotalNetSalesRevenue.Amount);
        }
    }
}
