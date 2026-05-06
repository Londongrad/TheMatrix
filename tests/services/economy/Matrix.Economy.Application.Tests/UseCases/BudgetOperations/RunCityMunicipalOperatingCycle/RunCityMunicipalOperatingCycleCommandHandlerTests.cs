using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;

public sealed class RunCityMunicipalOperatingCycleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExecutesMunicipalOperatingCycleAndPublishesPressureSignal()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBudget budget = CreateBudget(cityId);
        budget.ApplyLedgerEntry(CreateBudgetEntry(cityId, CityBudgetLedgerEntryKind.Revenue, 600m, "Opening Revenue"));
        var allocation = CreateAllocation(cityId, CityBudgetCategory.Infrastructure, 400m);
        CityBusiness business = CreateBusiness(cityId, "Bridge Vendor", CityBusinessKind.MunicipalVendor, 120m);
        var allocationRepository = new FakeCityBudgetAllocationRepository { Allocations = [allocation] };
        var budgetRepository = new FakeCityBudgetRepository { BudgetByCity = budget };
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository();
        var householdAccountRepository = new FakeCityHouseholdAccountRepository();
        var obligationRepository = new FakeCityHouseholdObligationRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
        var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 18, 18, 0, TimeSpan.Zero));
        var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
            allocationRepository,
            timeProvider);
        var chargeSupport = new HouseholdObligationChargeSupport(
            householdAccountRepository,
            new FakeCityHouseholdAccountLedgerRepository(),
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
        var handler = new RunCityMunicipalOperatingCycleCommandHandler(
            recurringCycleExecutionService,
            unitOfWork,
            signalPublisher,
            pressureProjectionService,
            timeProvider);

        RunCityMunicipalOperatingCycleResultDto result = await handler.Handle(
            new RunCityMunicipalOperatingCycleCommand(cityId),
            CancellationToken.None);

        var budgetEntry = Assert.Single(budgetLedgerRepository.AddedEntries);
        var businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
        var signal = Assert.Single(signalPublisher.PublishedSignals);
        Assert.Equal(timeProvider.UtcNow, budgetEntry.OccurredAtUtc);
        Assert.Equal(timeProvider.UtcNow, businessEntry.OccurredAtUtc);
        Assert.Equal(560m, budget.Balance.Amount);
        Assert.Equal(40m, budget.TotalCityExpenses.Amount);
        Assert.Equal(160m, business.Balance.Amount);
        Assert.Equal(40m, allocation.TotalSpent.Amount);
        Assert.Equal(timeProvider.UtcNow, allocation.UpdatedAtUtc);
        Assert.Equal(cityId, pressureProjectionService.RequestedCityId);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, signal.EffectiveAtUtc);
        Assert.Equal(timeProvider.UtcNow, signal.OccurredAtUtc);
        Assert.Equal(1, result.AllocationCategoriesTouched);
        Assert.Equal(1, result.ProviderPayments);
        Assert.Equal(40m, result.TotalDisbursedAmount);
    }
}
