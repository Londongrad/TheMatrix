using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;

public sealed class RunCityHouseholdBillingCycleCommandHandlerTests
{
    [Fact]
    public async Task Handle_UsesFrozenTimeFallbackAndPublishesFinancialStressBatch()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness providerBusiness = CreateBusiness(cityId, "City Utility", CityBusinessKind.Utility, 200m);
        CityHouseholdAccount householdAccount = CreateHouseholdAccount(cityId, "Tenant Household", 0m);
        CityHouseholdObligation obligation = CreateHouseholdObligation(
            cityId,
            householdAccount.Id,
            providerBusiness.Id,
            "Utility Bill",
            CityHouseholdObligationKind.Utilities,
            CityHouseholdObligationBillingCadence.Monthly,
            80m,
            8m);
        var allocationRepository = new FakeCityBudgetAllocationRepository();
        var budgetRepository = new FakeCityBudgetRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = [providerBusiness] };
        var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository();
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = [householdAccount] };
        var obligationRepository = new FakeCityHouseholdObligationRepository { Obligations = [obligation] };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var populationSignalPublisher = new FakeCityPopulationSignalPublisher();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 19, 48, 0, TimeSpan.Zero));
        var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
            allocationRepository,
            timeProvider);
        var chargeSupport = new HouseholdObligationChargeSupport(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            timeProvider);
        var taxRemittanceSupport = new CityBusinessTaxRemittanceSupport(
            businessLedgerRepository,
            budgetRepository,
            budgetLedgerRepository,
            timeProvider);
        var disbursementSupport = new CityBudgetBusinessDisbursementSupport(
            budgetRepository,
            budgetLedgerRepository,
            businessLedgerRepository,
            allocationExpenseSupport,
            timeProvider);
        var recurringCycleExecutionService = new CityEconomyRecurringCycleExecutionService(
            allocationRepository,
            budgetRepository,
            businessRepository,
            costProfileStateRepository,
            householdAccountRepository,
            obligationRepository,
            chargeSupport,
            taxRemittanceSupport,
            new CityEconomyCostProfilePolicy(),
            new CityEconomyServiceQualityPolicy(),
            new CityMunicipalOperatingCyclePolicy(),
            disbursementSupport);
        var handler = new RunCityHouseholdBillingCycleCommandHandler(
            populationSignalPublisher,
            recurringCycleExecutionService,
            unitOfWork,
            timeProvider);

        RunCityHouseholdBillingCycleResultDto result = await handler.Handle(
            new RunCityHouseholdBillingCycleCommand(cityId, null),
            CancellationToken.None);

        var batch = Assert.Single(populationSignalPublisher.HouseholdFinancialStressBatches);
        Assert.Equal(cityId, batch.CityId);
        Assert.Equal(timeProvider.UtcNow, batch.OccurredAtUtc);
        Assert.Single(batch.Households);
        Assert.Equal(householdAccount.Id, batch.Households[0].HouseholdAccountId);
        Assert.Equal(0, result.ChargedObligations);
        Assert.Equal(0m, result.TotalChargedAmount);
        Assert.Equal(0m, result.TotalTaxAmount);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.AsOfUtc);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Empty(householdLedgerRepository.AddedEntries);
        Assert.Empty(businessLedgerRepository.AddedEntries);
    }
}
