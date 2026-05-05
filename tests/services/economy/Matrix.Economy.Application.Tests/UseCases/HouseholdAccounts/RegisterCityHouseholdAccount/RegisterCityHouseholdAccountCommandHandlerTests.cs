using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdAccounts.RegisterCityHouseholdAccount;

public sealed class RegisterCityHouseholdAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesAccountWithFrozenTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var repository = new FakeCityHouseholdAccountRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 14, 5, 0, TimeSpan.Zero));
        var handler = new RegisterCityHouseholdAccountCommandHandler(repository, unitOfWork, timeProvider);
        var command = new RegisterCityHouseholdAccountCommand(
            CityId: cityId,
            Name: "  Anderson Household  ",
            ExternalReferenceCode: "  hh-001  ",
            OpeningBalance: 275m,
            UnitKind: null,
            UnitCode: null,
            UnitDisplayName: null,
            UnitSymbol: null);

        CityHouseholdAccountDto result = await handler.Handle(command, CancellationToken.None);

        var account = Assert.Single(repository.AddedAccounts);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, account.CreatedAtUtc);
        Assert.Equal(result.HouseholdAccountId, account.Id);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.CreatedAtUtc);
        Assert.Equal("Anderson Household", result.Name);
        Assert.Equal("hh-001", result.ExternalReferenceCode);
        Assert.Equal("Currency", result.UnitKind);
        Assert.Equal("MNY", result.UnitCode);
        Assert.Equal(275m, result.Balance);
        Assert.Equal(275m, result.TotalOpeningBalance);
    }

    [Fact]
    public async Task Handle_UsesExplicitUnitProfile()
    {
        Guid cityId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var repository = new FakeCityHouseholdAccountRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 6, 2, 9, 15, 0, TimeSpan.Zero));
        var handler = new RegisterCityHouseholdAccountCommandHandler(repository, unitOfWork, timeProvider);
        var command = new RegisterCityHouseholdAccountCommand(
            CityId: cityId,
            Name: "Co-op Household",
            ExternalReferenceCode: null,
            OpeningBalance: 40m,
            UnitKind: "Commodity",
            UnitCode: "grain",
            UnitDisplayName: "Grain",
            UnitSymbol: "kg");

        CityHouseholdAccountDto result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Commodity", result.UnitKind);
        Assert.Equal("GRAIN", result.UnitCode);
        Assert.Equal("Grain", result.UnitDisplayName);
        Assert.Equal("kg", result.UnitSymbol);
        Assert.Equal(40m, result.Balance);
    }
}
