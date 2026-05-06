using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetAllocations.SetCityBudgetAllocation;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.BudgetAllocations.SetCityBudgetAllocation;

public sealed class SetCityBudgetAllocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesAllocationAndPublishesPressureSignal()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var budgetRepository = new FakeCityBudgetRepository();
        var allocationRepository = new FakeCityBudgetAllocationRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
        var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 15, 45, 0, TimeSpan.Zero));
        var handler = new SetCityBudgetAllocationCommandHandler(
            budgetRepository,
            allocationRepository,
            unitOfWork,
            signalPublisher,
            pressureProjectionService,
            timeProvider);
        var command = new SetCityBudgetAllocationCommand(
            CityId: cityId,
            Category: CityBudgetCategory.Healthcare,
            TargetAmount: 180m,
            UnitKind: null,
            UnitCode: null,
            UnitDisplayName: null,
            UnitSymbol: null);

        var result = await handler.Handle(command, CancellationToken.None);

        CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);
        var allocation = Assert.Single(allocationRepository.AddedAllocations);
        var signal = Assert.Single(signalPublisher.PublishedSignals);
        Assert.Equal(cityId, budget.CityId);
        Assert.Equal(cityId, allocation.CityId);
        Assert.Equal(CityBudgetCategory.Healthcare, allocation.Category);
        Assert.Equal(180m, allocation.TargetAmount.Amount);
        Assert.Equal(timeProvider.UtcNow, allocation.CreatedAtUtc);
        Assert.Equal(timeProvider.UtcNow, allocation.UpdatedAtUtc);
        Assert.Equal(timeProvider.UtcNow, signal.EffectiveAtUtc);
        Assert.Equal(timeProvider.UtcNow, signal.OccurredAtUtc);
        Assert.Equal(cityId, pressureProjectionService.RequestedCityId);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Equal(180m, result.TargetAmount);
        Assert.Equal("Healthcare", result.Category);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.CreatedAtUtc);
    }

    [Fact]
    public async Task Handle_UpdatesExistingAllocationAndPreservesCreatedTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBudget budget = CreateBudget(cityId);
        var allocation = CreateAllocation(cityId, CityBudgetCategory.Operations, 120m, spentAmount: 35m);
        var budgetRepository = new FakeCityBudgetRepository { BudgetByCity = budget };
        var allocationRepository = new FakeCityBudgetAllocationRepository { Allocations = [allocation] };
        var unitOfWork = new FakeEconomyUnitOfWork();
        var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
        var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 16, 20, 0, TimeSpan.Zero));
        var handler = new SetCityBudgetAllocationCommandHandler(
            budgetRepository,
            allocationRepository,
            unitOfWork,
            signalPublisher,
            pressureProjectionService,
            timeProvider);
        DateTimeOffset createdAtUtc = allocation.CreatedAtUtc;
        var command = new SetCityBudgetAllocationCommand(
            CityId: cityId,
            Category: CityBudgetCategory.Operations,
            TargetAmount: 260m,
            UnitKind: null,
            UnitCode: null,
            UnitDisplayName: null,
            UnitSymbol: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Empty(allocationRepository.AddedAllocations);
        Assert.Equal(260m, allocation.TargetAmount.Amount);
        Assert.Equal(35m, allocation.TotalSpent.Amount);
        Assert.Equal(createdAtUtc, allocation.CreatedAtUtc);
        Assert.Equal(timeProvider.UtcNow, allocation.UpdatedAtUtc);
        Assert.Single(signalPublisher.PublishedSignals);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Equal(225m, result.AvailableAmount);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.UpdatedAtUtc);
    }
}
