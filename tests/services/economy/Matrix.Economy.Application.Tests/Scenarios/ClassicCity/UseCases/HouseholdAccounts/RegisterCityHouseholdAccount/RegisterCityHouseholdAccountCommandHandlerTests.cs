using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount;
using Matrix.Economy.Domain.Aggregates;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount
{
    public sealed class RegisterCityHouseholdAccountCommandHandlerTests
    {
        [Fact]
        public async Task Handle_CreatesAccountWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var repository = new FakeCityHouseholdAccountRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 14,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RegisterCityHouseholdAccountCommandHandler(
                householdAccountRepository: repository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RegisterCityHouseholdAccountCommand(
                CityId: cityId,
                Name: "  Anderson Household  ",
                ExternalReferenceCode: "  hh-001  ",
                OpeningBalance: 275m,
                UnitKind: null,
                UnitCode: null,
                UnitDisplayName: null,
                UnitSymbol: null);

            CityHouseholdAccountDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityHouseholdAccount account = Assert.Single(repository.AddedAccounts);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: account.CreatedAtUtc);
            Assert.Equal(
                expected: result.HouseholdAccountId,
                actual: account.Id);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: "Anderson Household",
                actual: result.Name);
            Assert.Equal(
                expected: "hh-001",
                actual: result.ExternalReferenceCode);
            Assert.Equal(
                expected: "Currency",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "MNY",
                actual: result.UnitCode);
            Assert.Equal(
                expected: 275m,
                actual: result.Balance);
            Assert.Equal(
                expected: 275m,
                actual: result.TotalOpeningBalance);
        }

        [Fact]
        public async Task Handle_UsesExplicitUnitProfile()
        {
            var cityId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var repository = new FakeCityHouseholdAccountRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 2,
                    hour: 9,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RegisterCityHouseholdAccountCommandHandler(
                householdAccountRepository: repository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RegisterCityHouseholdAccountCommand(
                CityId: cityId,
                Name: "Co-op Household",
                ExternalReferenceCode: null,
                OpeningBalance: 40m,
                UnitKind: "Commodity",
                UnitCode: "grain",
                UnitDisplayName: "Grain",
                UnitSymbol: "kg");

            CityHouseholdAccountDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Commodity",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "GRAIN",
                actual: result.UnitCode);
            Assert.Equal(
                expected: "Grain",
                actual: result.UnitDisplayName);
            Assert.Equal(
                expected: "kg",
                actual: result.UnitSymbol);
            Assert.Equal(
                expected: 40m,
                actual: result.Balance);
        }
    }
}
