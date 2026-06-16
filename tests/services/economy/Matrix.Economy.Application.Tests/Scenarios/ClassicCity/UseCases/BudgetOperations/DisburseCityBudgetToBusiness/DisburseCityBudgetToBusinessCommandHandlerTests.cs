using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.DisburseCityBudgetToBusiness;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.BudgetOperations.DisburseCityBudgetToBusiness
{
    public sealed class DisburseCityBudgetToBusinessCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DisbursesBudgetToBusinessAndPublishesPressureSignal()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBudget budget = CreateBudget(cityId);
            budget.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityId,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    amount: 700m,
                    title: "Opening Revenue"));
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Transit Contractor",
                kind: CityBusinessKind.MunicipalVendor,
                initialCapital: 150m);
            CityBudgetAllocation allocation = CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Infrastructure,
                targetAmount: 400m,
                spentAmount: 25m);
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var budgetRepository = new FakeCityBudgetRepository
            {
                BudgetByCity = budget
            };
            var allocationRepository = new FakeCityBudgetAllocationRepository
            {
                Allocations = [allocation]
            };
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
            var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 17,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero));
            var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
                allocationRepository: allocationRepository,
                timeProvider: timeProvider);
            var disbursementSupport = new CityBudgetBusinessDisbursementSupport(
                budgetRepository: budgetRepository,
                budgetLedgerRepository: budgetLedgerRepository,
                businessLedgerRepository: businessLedgerRepository,
                allocationExpenseSupport: allocationExpenseSupport,
                timeProvider: timeProvider);
            var handler = new DisburseCityBudgetToBusinessCommandHandler(
                businessRepository: businessRepository,
                disbursementSupport: disbursementSupport,
                unitOfWork: unitOfWork,
                operationalBudgetSignalPublisher: signalPublisher,
                pressureProjectionService: pressureProjectionService,
                timeProvider: timeProvider);
            var command = new DisburseCityBudgetToBusinessCommand(
                CityId: cityId,
                BusinessId: business.Id,
                Category: CityBudgetCategory.Infrastructure,
                Amount: 90m,
                Title: "Bridge Contractor Payment",
                Description: "Weekly milestone");

            BudgetLedgerEntryDto result = await handler.Handle(
                request: command,
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
                expected: 610m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: 90m,
                actual: budget.TotalCityExpenses.Amount);
            Assert.Equal(
                expected: 240m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 115m,
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
                expected: "Expense",
                actual: result.Kind);
            Assert.Equal(
                expected: "MunicipalDisbursement",
                actual: result.Source);
            Assert.Equal(
                expected: 90m,
                actual: result.Amount);
            Assert.Equal(
                expected: business.Id.ToString("N"),
                actual: result.ReferenceCode);
        }

        [Fact]
        public async Task Handle_ThrowsWhenBusinessBelongsToAnotherCity()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: Guid.Parse("ffffffff-1111-2222-3333-444444444444"),
                name: "Outsider Vendor",
                kind: CityBusinessKind.MunicipalVendor,
                initialCapital: 50m);
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [business]
            };
            var handler = new DisburseCityBudgetToBusinessCommandHandler(
                businessRepository: businessRepository,
                disbursementSupport: new CityBudgetBusinessDisbursementSupport(
                    budgetRepository: new FakeCityBudgetRepository(),
                    budgetLedgerRepository: new FakeCityBudgetLedgerRepository(),
                    businessLedgerRepository: new FakeCityBusinessLedgerRepository(),
                    allocationExpenseSupport: new CityBudgetAllocationExpenseSupport(
                        allocationRepository: new FakeCityBudgetAllocationRepository(),
                        timeProvider: new FrozenTimeProvider(
                            new DateTimeOffset(
                                year: 2048,
                                month: 5,
                                day: 8,
                                hour: 17,
                                minute: 40,
                                second: 0,
                                offset: TimeSpan.Zero))),
                    timeProvider: new FrozenTimeProvider(
                        new DateTimeOffset(
                            year: 2048,
                            month: 5,
                            day: 8,
                            hour: 17,
                            minute: 40,
                            second: 0,
                            offset: TimeSpan.Zero))),
                unitOfWork: new FakeEconomyUnitOfWork(),
                operationalBudgetSignalPublisher: new FakeCityOperationalBudgetSignalPublisher(),
                pressureProjectionService: new FakeCityOperationalBudgetPressureProjectionService(),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 8,
                        hour: 17,
                        minute: 40,
                        second: 0,
                        offset: TimeSpan.Zero)));
            var command = new DisburseCityBudgetToBusinessCommand(
                CityId: cityId,
                BusinessId: business.Id,
                Category: CityBudgetCategory.General,
                Amount: 25m,
                Title: "Invalid Transfer",
                Description: null);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => handler.Handle(
                    request: command,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Business and budget must belong to the same city.",
                actual: exception.Message);
        }
    }
}
