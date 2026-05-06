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

}
