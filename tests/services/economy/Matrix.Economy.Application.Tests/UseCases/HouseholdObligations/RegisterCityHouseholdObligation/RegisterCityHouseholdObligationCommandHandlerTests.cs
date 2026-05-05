using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RegisterCityHouseholdObligation;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdObligations.RegisterCityHouseholdObligation;

public sealed class RegisterCityHouseholdObligationCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesObligationWithDefaultDueDateFromFrozenClock()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Tenant", 200m);
        CityBusiness providerBusiness = CreateBusiness(cityId, "Landlord", CityBusinessKind.Landlord, 500m);
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var businessRepository = new FakeCityBusinessRepository { Businesses = [providerBusiness] };
        var obligationRepository = new FakeCityHouseholdObligationRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 18, 45, 0, TimeSpan.Zero));
        var handler = new RegisterCityHouseholdObligationCommandHandler(
            householdAccountRepository,
            businessRepository,
            obligationRepository,
            unitOfWork,
            timeProvider);
        var command = new RegisterCityHouseholdObligationCommand(
            CityId: cityId,
            HouseholdAccountId: householdAccount.Id,
            ProviderBusinessId: providerBusiness.Id,
            Name: "Monthly Rent",
            Kind: CityHouseholdObligationKind.Rent,
            BillingCadence: CityHouseholdObligationBillingCadence.Monthly,
            ChargeAmount: 80m,
            TaxAmount: 8m,
            FirstChargeDueAtUtc: null);

        CityHouseholdObligationDto result = await handler.Handle(command, CancellationToken.None);

        var obligation = Assert.Single(obligationRepository.AddedObligations);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, obligation.CreatedAtUtc);
        Assert.Equal(timeProvider.UtcNow.AddMonths(1), obligation.NextChargeDueAtUtc);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.CreatedAtUtc);
        Assert.Equal(timeProvider.UtcNow.AddMonths(1).ToString("O"), result.NextChargeDueAtUtc);
        Assert.Equal("Rent", result.Kind);
        Assert.Equal("Monthly", result.BillingCadence);
        Assert.Equal(80m, result.ChargeAmount);
        Assert.Equal(8m, result.TaxAmount);
        Assert.Equal(householdAccount.Id, result.HouseholdAccountId);
        Assert.Equal(providerBusiness.Id, result.ProviderBusinessId);
    }

    [Fact]
    public async Task Handle_ThrowsWhenActorsBelongToDifferentCities()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Tenant", 200m);
        CityBusiness providerBusiness = CreateBusiness(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            "Regional Utility",
            CityBusinessKind.Utility,
            500m);
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var businessRepository = new FakeCityBusinessRepository { Businesses = [providerBusiness] };
        var obligationRepository = new FakeCityHouseholdObligationRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 18, 45, 0, TimeSpan.Zero));
        var handler = new RegisterCityHouseholdObligationCommandHandler(
            householdAccountRepository,
            businessRepository,
            obligationRepository,
            unitOfWork,
            timeProvider);
        var command = new RegisterCityHouseholdObligationCommand(
            CityId: cityId,
            HouseholdAccountId: householdAccount.Id,
            ProviderBusinessId: providerBusiness.Id,
            Name: "Cross-city Utility",
            Kind: CityHouseholdObligationKind.Utilities,
            BillingCadence: CityHouseholdObligationBillingCadence.Monthly,
            ChargeAmount: 40m,
            TaxAmount: 4m,
            FirstChargeDueAtUtc: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Obligation actors must belong to the same city.", exception.Message);
        Assert.Empty(obligationRepository.AddedObligations);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
