using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.IssueHouseholdObligationCharge;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdObligations.IssueHouseholdObligationCharge;

public sealed class IssueHouseholdObligationChargeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ChargesObligationAndSavesChanges()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Tenant Household", 300m);
        CityBusiness providerBusiness = CreateBusiness(cityId, "Landlord", CityBusinessKind.Landlord, 500m);
        CityHouseholdObligation obligation = CreateHouseholdObligation(
            cityId,
            householdAccount.Id,
            providerBusiness.Id,
            "Monthly Rent",
            CityHouseholdObligationKind.Rent,
            CityHouseholdObligationBillingCadence.Monthly,
            80m,
            8m);
        var obligationRepository = new FakeCityHouseholdObligationRepository { Obligations = [obligation] };
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = [providerBusiness] };
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 10, 0, 0, TimeSpan.Zero));
        var chargeSupport = new HouseholdObligationChargeSupport(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            timeProvider);
        var unitOfWork = new FakeEconomyUnitOfWork();
        var handler = new IssueHouseholdObligationChargeCommandHandler(
            obligationRepository,
            chargeSupport,
            unitOfWork);
        var command = new IssueHouseholdObligationChargeCommand(
            ObligationId: obligation.Id,
            Description: "Scheduled charge");

        CityHouseholdAccountLedgerEntryDto result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal("ObligationCharge", result.Kind);
        Assert.Equal("Obligation", result.Source);
        Assert.Equal(80m, result.Amount);
        Assert.Equal(obligation.Id.ToString("N"), result.ReferenceCode);
        Assert.Single(householdLedgerRepository.AddedEntries);
        Assert.Single(businessLedgerRepository.AddedEntries);
    }

    [Fact]
    public async Task Handle_ThrowsWhenObligationIsMissing()
    {
        var obligationRepository = new FakeCityHouseholdObligationRepository();
        var chargeSupport = new HouseholdObligationChargeSupport(
            new FakeCityHouseholdAccountRepository(),
            new FakeCityHouseholdAccountLedgerRepository(),
            new FakeCityBusinessRepository(),
            new FakeCityBusinessLedgerRepository(),
            new FrozenTimeProvider(new DateTimeOffset(2048, 5, 7, 10, 0, 0, TimeSpan.Zero)));
        var unitOfWork = new FakeEconomyUnitOfWork();
        var handler = new IssueHouseholdObligationChargeCommandHandler(
            obligationRepository,
            chargeSupport,
            unitOfWork);
        var obligationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new IssueHouseholdObligationChargeCommand(obligationId, null), CancellationToken.None));

        Assert.Equal($"Obligation '{obligationId}' was not found.", exception.Message);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
