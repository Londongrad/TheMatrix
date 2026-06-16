using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed class RunCityMunicipalOperatingCycleCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ExecutesMunicipalOperatingCycleAndPublishesPressureSignal()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBudget budget = CreateBudget(cityId);
            budget.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityId,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    amount: 600m,
                    title: "Opening Revenue"));
            CityBudgetAllocation allocation = CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Infrastructure,
                targetAmount: 400m);
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Bridge Vendor",
                kind: CityBusinessKind.MunicipalVendor,
                initialCapital: 120m);
            var allocationRepository = new FakeCityBudgetAllocationRepository
            {
                Allocations = [allocation]
            };
            var budgetRepository = new FakeCityBudgetRepository
            {
                BudgetByCity = budget
            };
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository();
            var householdAccountRepository = new FakeCityHouseholdAccountRepository();
            var obligationRepository = new FakeCityHouseholdObligationRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
            var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 18,
                    minute: 18,
                    second: 0,
                    offset: TimeSpan.Zero));
            var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
                allocationRepository: allocationRepository,
                timeProvider: timeProvider);
            var chargeSupport = new HouseholdObligationChargeSupport(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: new FakeCityHouseholdAccountLedgerRepository(),
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                timeProvider: timeProvider);
            var taxRemittanceSupport = new CityBusinessTaxRemittanceSupport(
                businessLedgerRepository: businessLedgerRepository,
                budgetRepository: budgetRepository,
                budgetLedgerRepository: budgetLedgerRepository,
                timeProvider: timeProvider);
            var disbursementSupport = new CityBudgetBusinessDisbursementSupport(
                budgetRepository: budgetRepository,
                budgetLedgerRepository: budgetLedgerRepository,
                businessLedgerRepository: businessLedgerRepository,
                allocationExpenseSupport: allocationExpenseSupport,
                timeProvider: timeProvider);
            var recurringCycleExecutionService = new CityEconomyRecurringCycleExecutionService(
                allocationRepository: allocationRepository,
                budgetRepository: budgetRepository,
                businessRepository: businessRepository,
                costProfileStateRepository: costProfileStateRepository,
                householdAccountRepository: householdAccountRepository,
                obligationRepository: obligationRepository,
                chargeSupport: chargeSupport,
                taxRemittanceSupport: taxRemittanceSupport,
                costProfilePolicy: new CityEconomyCostProfilePolicy(),
                serviceQualityPolicy: new CityEconomyServiceQualityPolicy(),
                municipalOperatingCyclePolicy: new CityMunicipalOperatingCyclePolicy(),
                disbursementSupport: disbursementSupport);
            var handler = new RunCityMunicipalOperatingCycleCommandHandler(
                recurringCycleExecutionService: recurringCycleExecutionService,
                unitOfWork: unitOfWork,
                operationalBudgetSignalPublisher: signalPublisher,
                pressureProjectionService: pressureProjectionService,
                timeProvider: timeProvider);

            RunCityMunicipalOperatingCycleResultDto result = await handler.Handle(
                request: new RunCityMunicipalOperatingCycleCommand(cityId),
                cancellationToken: CancellationToken.None);

            CityBudgetLedgerEntry budgetEntry = Assert.Single(budgetLedgerRepository.AddedEntries);
            CityBusinessLedgerEntry businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
            FakeCityOperationalBudgetSignalPublisher.PublishedSignal signal =
                Assert.Single(signalPublisher.PublishedSignals);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: budgetEntry.OccurredAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: businessEntry.OccurredAtUtc);
            Assert.Equal(
                expected: 560m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: 40m,
                actual: budget.TotalCityExpenses.Amount);
            Assert.Equal(
                expected: 160m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 40m,
                actual: allocation.TotalSpent.Amount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: allocation.UpdatedAtUtc);
            Assert.Equal(
                expected: cityId,
                actual: pressureProjectionService.RequestedCityId);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: signal.EffectiveAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: signal.OccurredAtUtc);
            Assert.Equal(
                expected: 1,
                actual: result.AllocationCategoriesTouched);
            Assert.Equal(
                expected: 1,
                actual: result.ProviderPayments);
            Assert.Equal(
                expected: 40m,
                actual: result.TotalDisbursedAmount);
        }
    }
}
