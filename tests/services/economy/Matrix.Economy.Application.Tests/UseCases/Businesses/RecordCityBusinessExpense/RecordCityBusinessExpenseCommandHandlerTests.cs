using Matrix.Economy.Application.UseCases.Businesses;
using Matrix.Economy.Application.UseCases.Businesses.RecordCityBusinessExpense;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RecordCityBusinessExpense
{
    public sealed class RecordCityBusinessExpenseCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RecordsExpenseWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Transit Depot",
                kind: CityBusinessKind.Service,
                initialCapital: 300m);
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
                    hour: 10,
                    minute: 45,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RecordCityBusinessExpenseCommandHandler(
                businessRepository: businessRepository,
                ledgerRepository: ledgerRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RecordCityBusinessExpenseCommand(
                BusinessId: business.Id,
                Amount: 75m,
                Title: "Fuel Purchase",
                Description: "Diesel refill");

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
                expected: "OperatingExpense",
                actual: result.Kind);
            Assert.Equal(
                expected: "Operations",
                actual: result.Source);
            Assert.Equal(
                expected: 75m,
                actual: result.Amount);
            Assert.Equal(
                expected: 0m,
                actual: result.TaxAmount);
            Assert.Equal(
                expected: 225m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 75m,
                actual: business.TotalOperatingExpenses.Amount);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.OccurredAtUtc);
        }
    }
}
