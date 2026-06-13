using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed class RunCityHouseholdBillingCycleCommandHandlerTests
    {
        [Fact]
        public async Task Handle_UsesFrozenTimeFallbackAndPublishesFinancialStressBatch()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBusiness providerBusiness = CreateBusiness(
                cityId: cityId,
                name: "City Utility",
                kind: CityBusinessKind.Utility,
                initialCapital: 200m);
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Tenant Household",
                openingBalance: 0m);
            CityHouseholdObligation obligation = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdAccount.Id,
                providerBusinessId: providerBusiness.Id,
                name: "Utility Bill",
                kind: CityHouseholdObligationKind.Utilities,
                cadence: CityHouseholdObligationBillingCadence.Monthly,
                chargeAmount: 80m,
                taxAmount: 8m);
            var allocationRepository = new FakeCityBudgetAllocationRepository();
            var budgetRepository = new FakeCityBudgetRepository();
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [providerBusiness]
            };
            var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository();
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var obligationRepository = new FakeCityHouseholdObligationRepository
            {
                Obligations = [obligation]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var populationSignalPublisher = new FakeCityPopulationSignalPublisher();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 19,
                    minute: 48,
                    second: 0,
                    offset: TimeSpan.Zero));
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
            var handler = new RunCityHouseholdBillingCycleCommandHandler(
                cityPopulationSignalPublisher: populationSignalPublisher,
                recurringCycleExecutionService: recurringCycleExecutionService,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);

            RunCityHouseholdBillingCycleResultDto result = await handler.Handle(
                request: new RunCityHouseholdBillingCycleCommand(
                    CityId: cityId,
                    AsOfUtc: null),
                cancellationToken: CancellationToken.None);

            ClassicCityHouseholdFinancialStressBatchV1 batch = Assert.Single(
                populationSignalPublisher.HouseholdFinancialStressBatches);
            Assert.Equal(
                expected: cityId,
                actual: batch.CityId);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: batch.OccurredAtUtc);
            Assert.Single(batch.Households);
            Assert.Equal(
                expected: householdAccount.Id,
                actual: batch.Households[0].HouseholdAccountId);
            Assert.Equal(
                expected: 0,
                actual: result.ChargedObligations);
            Assert.Equal(
                expected: 0m,
                actual: result.TotalChargedAmount);
            Assert.Equal(
                expected: 0m,
                actual: result.TotalTaxAmount);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.AsOfUtc);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Empty(householdLedgerRepository.AddedEntries);
            Assert.Empty(businessLedgerRepository.AddedEntries);
        }
    }
}
