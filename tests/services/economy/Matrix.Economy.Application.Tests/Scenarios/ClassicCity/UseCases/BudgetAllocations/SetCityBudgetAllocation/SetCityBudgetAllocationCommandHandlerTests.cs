using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.SetCityBudgetAllocation;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.BudgetAllocations.SetCityBudgetAllocation
{
    public sealed class SetCityBudgetAllocationCommandHandlerTests
    {
        [Fact]
        public async Task Handle_CreatesAllocationAndPublishesPressureSignal()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var budgetRepository = new FakeCityBudgetRepository();
            var allocationRepository = new FakeCityBudgetAllocationRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
            var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 15,
                    minute: 45,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new SetCityBudgetAllocationCommandHandler(
                budgetRepository: budgetRepository,
                allocationRepository: allocationRepository,
                unitOfWork: unitOfWork,
                operationalBudgetSignalPublisher: signalPublisher,
                pressureProjectionService: pressureProjectionService,
                timeProvider: timeProvider);
            var command = new SetCityBudgetAllocationCommand(
                CityId: cityId,
                Category: CityBudgetCategory.Healthcare,
                TargetAmount: 180m,
                UnitKind: null,
                UnitCode: null,
                UnitDisplayName: null,
                UnitSymbol: null);

            CityBudgetAllocationDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);
            CityBudgetAllocation allocation = Assert.Single(allocationRepository.AddedAllocations);
            FakeCityOperationalBudgetSignalPublisher.PublishedSignal signal =
                Assert.Single(signalPublisher.PublishedSignals);
            Assert.Equal(
                expected: cityId,
                actual: budget.CityId);
            Assert.Equal(
                expected: cityId,
                actual: allocation.CityId);
            Assert.Equal(
                expected: CityBudgetCategory.Healthcare,
                actual: allocation.Category);
            Assert.Equal(
                expected: 180m,
                actual: allocation.TargetAmount.Amount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: allocation.CreatedAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: allocation.UpdatedAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: signal.EffectiveAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: signal.OccurredAtUtc);
            Assert.Equal(
                expected: cityId,
                actual: pressureProjectionService.RequestedCityId);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 180m,
                actual: result.TargetAmount);
            Assert.Equal(
                expected: "Healthcare",
                actual: result.Category);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.CreatedAtUtc);
        }

        [Fact]
        public async Task Handle_UpdatesExistingAllocationAndPreservesCreatedTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBudget budget = CreateBudget(cityId);
            CityBudgetAllocation allocation = CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Operations,
                targetAmount: 120m,
                spentAmount: 35m);
            var budgetRepository = new FakeCityBudgetRepository
            {
                BudgetByCity = budget
            };
            var allocationRepository = new FakeCityBudgetAllocationRepository
            {
                Allocations = [allocation]
            };
            var unitOfWork = new FakeEconomyUnitOfWork();
            var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
            var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 16,
                    minute: 20,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new SetCityBudgetAllocationCommandHandler(
                budgetRepository: budgetRepository,
                allocationRepository: allocationRepository,
                unitOfWork: unitOfWork,
                operationalBudgetSignalPublisher: signalPublisher,
                pressureProjectionService: pressureProjectionService,
                timeProvider: timeProvider);
            DateTimeOffset createdAtUtc = allocation.CreatedAtUtc;
            var command = new SetCityBudgetAllocationCommand(
                CityId: cityId,
                Category: CityBudgetCategory.Operations,
                TargetAmount: 260m,
                UnitKind: null,
                UnitCode: null,
                UnitDisplayName: null,
                UnitSymbol: null);

            CityBudgetAllocationDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            Assert.Empty(allocationRepository.AddedAllocations);
            Assert.Equal(
                expected: 260m,
                actual: allocation.TargetAmount.Amount);
            Assert.Equal(
                expected: 35m,
                actual: allocation.TotalSpent.Amount);
            Assert.Equal(
                expected: createdAtUtc,
                actual: allocation.CreatedAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: allocation.UpdatedAtUtc);
            Assert.Single(signalPublisher.PublishedSignals);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: 225m,
                actual: result.AvailableAmount);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.UpdatedAtUtc);
        }
    }
}
