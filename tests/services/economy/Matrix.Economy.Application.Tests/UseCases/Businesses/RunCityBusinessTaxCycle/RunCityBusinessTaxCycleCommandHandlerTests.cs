using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Businesses.RunCityBusinessTaxCycle;

public sealed class RunCityBusinessTaxCycleCommandHandlerTests
{
    private static (
        RunCityBusinessTaxCycleCommandHandler Handler,
        FakeCityBusinessRepository BusinessRepository,
        FakeCityBusinessLedgerRepository BusinessLedgerRepository,
        FakeCityBudgetRepository BudgetRepository,
        FakeCityBudgetLedgerRepository BudgetLedgerRepository,
        FakeEconomyUnitOfWork UnitOfWork,
        FrozenTimeProvider TimeProvider) CreateSut(
        IReadOnlyList<CityBusiness> businesses,
        DateTimeOffset utcNow)
    {
        var allocationRepository = new FakeCityBudgetAllocationRepository();
        var budgetRepository = new FakeCityBudgetRepository();
        var businessRepository = new FakeCityBusinessRepository { Businesses = businesses };
        var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository();
        var householdAccountRepository = new FakeCityHouseholdAccountRepository();
        var obligationRepository = new FakeCityHouseholdObligationRepository();
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(utcNow);
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
        var handler = new RunCityBusinessTaxCycleCommandHandler(
            recurringCycleExecutionService,
            unitOfWork);

        return (
            handler,
            businessRepository,
            businessLedgerRepository,
            budgetRepository,
            budgetLedgerRepository,
            unitOfWork,
            timeProvider);
    }

    [Fact]
    public async Task Handle_RemitsTaxReserveForEligibleBusiness()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Corner Store", CityBusinessKind.RetailStore, 200m);
        business.RecordRetailSale(
            grossAmount: Money.FromDecimal(50m),
            salesTaxAmount: Money.FromDecimal(5m));
        var sut = CreateSut(
            businesses: [business],
            utcNow: new DateTimeOffset(2048, 5, 8, 20, 34, 0, TimeSpan.Zero));

        RunCityBusinessTaxCycleResultDto result = await sut.Handler.Handle(
            new RunCityBusinessTaxCycleCommand(
                CityId: cityId,
                BudgetCategory: CityBudgetCategory.Taxation),
            CancellationToken.None);

        CityBusinessLedgerEntry businessEntry = Assert.Single(sut.BusinessLedgerRepository.AddedEntries);
        CityBudgetLedgerEntry budgetEntry = Assert.Single(sut.BudgetLedgerRepository.AddedEntries);
        CityBudget budget = Assert.Single(sut.BudgetRepository.AddedBudgets);
        Assert.Equal(sut.TimeProvider.UtcNow, businessEntry.OccurredAtUtc);
        Assert.Equal(sut.TimeProvider.UtcNow, budgetEntry.OccurredAtUtc);
        Assert.Equal(1, sut.UnitOfWork.SaveChangesCallCount);
        Assert.Equal(1, result.RemittedBusinesses);
        Assert.Equal(5m, result.TotalRemittedAmount);
        Assert.Equal("Taxation", result.BudgetCategory);
        Assert.Equal(245m, business.Balance.Amount);
        Assert.Equal(0m, business.TaxReserve.Amount);
        Assert.Equal(5m, business.TotalTaxRemitted.Amount);
        Assert.Equal(5m, budget.Balance.Amount);
        Assert.Equal(CityBudgetLedgerEntrySource.BusinessRemittance, budgetEntry.Source);
    }

    [Fact]
    public async Task Handle_SkipsBusinessesWithoutTaxReserve()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(cityId, "Workshop", CityBusinessKind.Service, 180m);
        var sut = CreateSut(
            businesses: [business],
            utcNow: new DateTimeOffset(2048, 5, 8, 21, 41, 0, TimeSpan.Zero));

        RunCityBusinessTaxCycleResultDto result = await sut.Handler.Handle(
            new RunCityBusinessTaxCycleCommand(
                CityId: cityId,
                BudgetCategory: CityBudgetCategory.General),
            CancellationToken.None);

        Assert.Empty(sut.BusinessLedgerRepository.AddedEntries);
        Assert.Empty(sut.BudgetLedgerRepository.AddedEntries);
        Assert.Empty(sut.BudgetRepository.AddedBudgets);
        Assert.Equal(1, sut.UnitOfWork.SaveChangesCallCount);
        Assert.Equal(0, result.RemittedBusinesses);
        Assert.Equal(0m, result.TotalRemittedAmount);
        Assert.Equal(180m, business.Balance.Amount);
        Assert.Equal(0m, business.TaxReserve.Amount);
    }

}
