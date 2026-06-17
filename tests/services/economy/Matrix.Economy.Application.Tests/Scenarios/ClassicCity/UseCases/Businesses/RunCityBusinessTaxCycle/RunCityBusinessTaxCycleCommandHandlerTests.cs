using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Businesses.RunCityBusinessTaxCycle
{
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
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = businesses
            };
            var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository();
            var householdAccountRepository = new FakeCityHouseholdAccountRepository();
            var obligationRepository = new FakeCityHouseholdObligationRepository();
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(utcNow);
            var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
                allocationRepository: allocationRepository,
                timeProvider: timeProvider);
            var chargeSupport = new HouseholdObligationChargeSupport(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
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
            var handler = new RunCityBusinessTaxCycleCommandHandler(
                recurringCycleExecutionService: recurringCycleExecutionService,
                unitOfWork: unitOfWork);

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
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Corner Store",
                kind: CityBusinessKind.RetailStore,
                initialCapital: 200m);
            business.RecordRetailSale(
                grossAmount: Money.FromDecimal(50m),
                salesTaxAmount: Money.FromDecimal(5m));
            (RunCityBusinessTaxCycleCommandHandler Handler, FakeCityBusinessRepository BusinessRepository,
                FakeCityBusinessLedgerRepository BusinessLedgerRepository, FakeCityBudgetRepository BudgetRepository,
                FakeCityBudgetLedgerRepository BudgetLedgerRepository, FakeEconomyUnitOfWork UnitOfWork,
                FrozenTimeProvider TimeProvider) sut = CreateSut(
                    businesses: [business],
                    utcNow: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 8,
                        hour: 20,
                        minute: 34,
                        second: 0,
                        offset: TimeSpan.Zero));

            RunCityBusinessTaxCycleResultDto result = await sut.Handler.Handle(
                request: new RunCityBusinessTaxCycleCommand(
                    CityId: cityId,
                    BudgetCategory: CityBudgetCategory.Taxation),
                cancellationToken: CancellationToken.None);

            CityBusinessLedgerEntry businessEntry = Assert.Single(sut.BusinessLedgerRepository.AddedEntries);
            CityBudgetLedgerEntry budgetEntry = Assert.Single(sut.BudgetLedgerRepository.AddedEntries);
            CityBudget budget = Assert.Single(sut.BudgetRepository.AddedBudgets);
            Assert.Equal(
                expected: sut.TimeProvider.UtcNow,
                actual: businessEntry.OccurredAtUtc);
            Assert.Equal(
                expected: sut.TimeProvider.UtcNow,
                actual: budgetEntry.OccurredAtUtc);
            Assert.Equal(
                expected: 1,
                actual: sut.UnitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 1,
                actual: result.RemittedBusinesses);
            Assert.Equal(
                expected: 5m,
                actual: result.TotalRemittedAmount);
            Assert.Equal(
                expected: "Taxation",
                actual: result.BudgetCategory);
            Assert.Equal(
                expected: 245m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 0m,
                actual: business.TaxReserve.Amount);
            Assert.Equal(
                expected: 5m,
                actual: business.TotalTaxRemitted.Amount);
            Assert.Equal(
                expected: 5m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: CityBudgetLedgerEntrySource.BusinessRemittance,
                actual: budgetEntry.Source);
        }

        [Fact]
        public async Task Handle_SkipsBusinessesWithoutTaxReserve()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness business = CreateBusiness(
                cityId: cityId,
                name: "Workshop",
                kind: CityBusinessKind.Service,
                initialCapital: 180m);
            (RunCityBusinessTaxCycleCommandHandler Handler, FakeCityBusinessRepository BusinessRepository,
                FakeCityBusinessLedgerRepository BusinessLedgerRepository, FakeCityBudgetRepository BudgetRepository,
                FakeCityBudgetLedgerRepository BudgetLedgerRepository, FakeEconomyUnitOfWork UnitOfWork,
                FrozenTimeProvider TimeProvider) sut = CreateSut(
                    businesses: [business],
                    utcNow: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 8,
                        hour: 21,
                        minute: 41,
                        second: 0,
                        offset: TimeSpan.Zero));

            RunCityBusinessTaxCycleResultDto result = await sut.Handler.Handle(
                request: new RunCityBusinessTaxCycleCommand(
                    CityId: cityId,
                    BudgetCategory: CityBudgetCategory.General),
                cancellationToken: CancellationToken.None);

            Assert.Empty(sut.BusinessLedgerRepository.AddedEntries);
            Assert.Empty(sut.BudgetLedgerRepository.AddedEntries);
            Assert.Empty(sut.BudgetRepository.AddedBudgets);
            Assert.Equal(
                expected: 1,
                actual: sut.UnitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 0,
                actual: result.RemittedBusinesses);
            Assert.Equal(
                expected: 0m,
                actual: result.TotalRemittedAmount);
            Assert.Equal(
                expected: 180m,
                actual: business.Balance.Amount);
            Assert.Equal(
                expected: 0m,
                actual: business.TaxReserve.Amount);
        }
    }
}
