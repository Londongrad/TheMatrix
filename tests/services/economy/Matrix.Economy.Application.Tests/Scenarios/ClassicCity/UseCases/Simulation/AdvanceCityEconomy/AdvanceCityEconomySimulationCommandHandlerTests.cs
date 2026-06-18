using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;
using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Simulation.AdvanceCityEconomy;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.Simulation.AdvanceCityEconomy
{
    public sealed class AdvanceCityEconomySimulationCommandHandlerTests
    {
        private static (
            AdvanceCityEconomySimulationCommandHandler Handler,
            FakeCityEconomyProgressionStateRepository ProgressionStateRepository,
            FakeCityPopulationSignalPublisher PopulationSignalPublisher,
            FakeEconomyUnitOfWork UnitOfWork,
            FrozenTimeProvider TimeProvider) CreateSut(
                Guid cityId,
                DateTimeOffset utcNow,
                CityEconomyProgressionState? progressionState = null,
                IReadOnlyList<CityBudgetAllocation>? allocations = null,
                IReadOnlyList<CityBusiness>? businesses = null,
                IReadOnlyList<CityHouseholdObligation>? obligations = null,
                IReadOnlyList<CityHouseholdAccount>? householdAccounts = null,
                CityBudget? budget = null,
                CityEconomyCostProfileState? costProfileState = null)
        {
            var allocationRepository = new FakeCityBudgetAllocationRepository
            {
                Allocations = allocations ?? Array.Empty<CityBudgetAllocation>()
            };
            var budgetRepository = new FakeCityBudgetRepository
            {
                BudgetByCity = budget
            };
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = businesses ?? Array.Empty<CityBusiness>()
            };
            var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository
            {
                StateByCity = costProfileState
            };
            var progressionStateRepository = new FakeCityEconomyProgressionStateRepository
            {
                StateByCity = progressionState
            };
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = householdAccounts ?? Array.Empty<CityHouseholdAccount>()
            };
            var obligationRepository = new FakeCityHouseholdObligationRepository
            {
                Obligations = obligations ?? Array.Empty<CityHouseholdObligation>()
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
            var populationSignalPublisher = new FakeCityPopulationSignalPublisher();
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
            var handler = new AdvanceCityEconomySimulationCommandHandler(
                progressionStateRepository: progressionStateRepository,
                cityPopulationSignalPublisher: populationSignalPublisher,
                recurringCycleExecutionService: recurringCycleExecutionService,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);

            return (
                handler,
                progressionStateRepository,
                populationSignalPublisher,
                unitOfWork,
                timeProvider);
        }

        [Fact]
        public async Task Handle_ReturnsDuplicateWhenTickAndDateWereAlreadyProcessed()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var existingState = CityEconomyProgressionState.Create(
                cityId: cityId,
                lastCompletedTickId: 7,
                lastProcessedDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 8),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            (AdvanceCityEconomySimulationCommandHandler Handler, FakeCityEconomyProgressionStateRepository
                ProgressionStateRepository, FakeCityPopulationSignalPublisher PopulationSignalPublisher,
                FakeEconomyUnitOfWork UnitOfWork, FrozenTimeProvider TimeProvider) sut = CreateSut(
                    cityId: cityId,
                    utcNow: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 8,
                        hour: 9,
                        minute: 14,
                        second: 0,
                        offset: TimeSpan.Zero),
                    progressionState: existingState);

            AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
                request: new AdvanceCityEconomySimulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 8,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 8,
                        hour: 12,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 7),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityEconomySimulationStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.ProcessedDays);
            Assert.Empty(sut.ProgressionStateRepository.AddedStates);
            Assert.Equal(
                expected: 0,
                actual: sut.UnitOfWork.SaveChangesCallCount);
            Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
            Assert.Empty(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
        }

        [Fact]
        public async Task Handle_ReturnsOutOfOrderWhenDateMovesBackwards()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var existingState = CityEconomyProgressionState.Create(
                cityId: cityId,
                lastCompletedTickId: 3,
                lastProcessedDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 10),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 10,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            (AdvanceCityEconomySimulationCommandHandler Handler, FakeCityEconomyProgressionStateRepository
                ProgressionStateRepository, FakeCityPopulationSignalPublisher PopulationSignalPublisher,
                FakeEconomyUnitOfWork UnitOfWork, FrozenTimeProvider TimeProvider) sut = CreateSut(
                    cityId: cityId,
                    utcNow: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 10,
                        hour: 9,
                        minute: 52,
                        second: 0,
                        offset: TimeSpan.Zero),
                    progressionState: existingState);

            AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
                request: new AdvanceCityEconomySimulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 8,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 9,
                        hour: 23,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 4),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityEconomySimulationStatus.OutOfOrder,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.ProcessedDays);
            Assert.Empty(sut.ProgressionStateRepository.AddedStates);
            Assert.Equal(
                expected: 0,
                actual: sut.UnitOfWork.SaveChangesCallCount);
            Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
            Assert.Empty(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
        }

        [Fact]
        public async Task Handle_AppliesSameDayTickWithoutRunningDailyCycles()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            (AdvanceCityEconomySimulationCommandHandler Handler, FakeCityEconomyProgressionStateRepository
                ProgressionStateRepository, FakeCityPopulationSignalPublisher PopulationSignalPublisher,
                FakeEconomyUnitOfWork UnitOfWork, FrozenTimeProvider TimeProvider) sut = CreateSut(
                    cityId: cityId,
                    utcNow: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 11,
                        hour: 10,
                        minute: 31,
                        second: 0,
                        offset: TimeSpan.Zero));

            AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
                request: new AdvanceCityEconomySimulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 11,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 11,
                        hour: 18,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 2),
                cancellationToken: CancellationToken.None);

            CityEconomyProgressionState state = Assert.Single(sut.ProgressionStateRepository.AddedStates);
            Assert.Equal(
                expected: AdvanceCityEconomySimulationStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.ProcessedDays);
            Assert.Equal(
                expected: 0,
                actual: result.ChargedObligations);
            Assert.Equal(
                expected: 0,
                actual: result.RemittedBusinesses);
            Assert.Equal(
                expected: 0,
                actual: result.MunicipalProviderPayments);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 11),
                actual: state.LastProcessedDate);
            Assert.Equal(
                expected: 2,
                actual: state.LastCompletedTickId);
            Assert.Equal(
                expected: sut.TimeProvider.UtcNow,
                actual: state.UpdatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: sut.UnitOfWork.SaveChangesCallCount);
            Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
            Assert.Empty(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
            Assert.Empty(sut.PopulationSignalPublisher.HouseholdFinancialStressBatches);
        }

        [Fact]
        public async Task Handle_AppliesDailyCycleAndPublishesServiceQualitySnapshot()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            (AdvanceCityEconomySimulationCommandHandler Handler, FakeCityEconomyProgressionStateRepository
                ProgressionStateRepository, FakeCityPopulationSignalPublisher PopulationSignalPublisher,
                FakeEconomyUnitOfWork UnitOfWork, FrozenTimeProvider TimeProvider) sut = CreateSut(
                    cityId: cityId,
                    utcNow: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 12,
                        hour: 11,
                        minute: 13,
                        second: 0,
                        offset: TimeSpan.Zero));

            AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
                request: new AdvanceCityEconomySimulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 11,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 12,
                        hour: 17,
                        minute: 30,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 5),
                cancellationToken: CancellationToken.None);

            CityEconomyProgressionState state = Assert.Single(sut.ProgressionStateRepository.AddedStates);
            ClassicCityServiceQualitySnapshotV1 snapshot =
                Assert.Single(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
            Assert.Equal(
                expected: AdvanceCityEconomySimulationStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: result.ProcessedDays);
            Assert.Equal(
                expected: 0,
                actual: result.ChargedObligations);
            Assert.Equal(
                expected: 0,
                actual: result.RemittedBusinesses);
            Assert.Equal(
                expected: 0,
                actual: result.MunicipalProviderPayments);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 12),
                actual: state.LastProcessedDate);
            Assert.Equal(
                expected: 5,
                actual: state.LastCompletedTickId);
            Assert.Equal(
                expected: sut.TimeProvider.UtcNow,
                actual: state.UpdatedAtUtc);
            Assert.Equal(
                expected: 3,
                actual: sut.UnitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: cityId,
                actual: snapshot.CityId);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 12,
                    hour: 17,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: snapshot.OccurredAtUtc);
            Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
            Assert.Empty(sut.PopulationSignalPublisher.HouseholdFinancialStressBatches);
        }
    }
}
