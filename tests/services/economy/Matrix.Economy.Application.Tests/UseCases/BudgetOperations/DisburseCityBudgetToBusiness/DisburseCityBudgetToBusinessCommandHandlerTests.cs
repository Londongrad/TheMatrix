using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.BudgetOperations.DisburseCityBudgetToBusiness;

public sealed class DisburseCityBudgetToBusinessCommandHandlerTests
{
    [Fact]
    public async Task Handle_DisbursesBudgetToBusinessAndPublishesPressureSignal()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBudget budget = CreateBudget(cityId);
        budget.ApplyLedgerEntry(CreateBudgetEntry(cityId, CityBudgetLedgerEntryKind.Revenue, 700m, "Opening Revenue"));
        CityBusiness business = CreateBusiness(cityId, "Transit Contractor", CityBusinessKind.MunicipalVendor, 150m);
        CityBudgetAllocation allocation = CreateAllocation(cityId, CityBudgetCategory.Infrastructure, 400m, spentAmount: 25m);
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var budgetRepository = new FakeCityBudgetRepository { BudgetByCity = budget };
        var allocationRepository = new FakeCityBudgetAllocationRepository { Allocations = [allocation] };
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
        var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 17, 5, 0, TimeSpan.Zero));
        var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
            allocationRepository,
            timeProvider);
        var disbursementSupport = new CityBudgetBusinessDisbursementSupport(
            budgetRepository,
            budgetLedgerRepository,
            businessLedgerRepository,
            allocationExpenseSupport,
            timeProvider);
        var handler = new DisburseCityBudgetToBusinessCommandHandler(
            businessRepository,
            disbursementSupport,
            unitOfWork,
            signalPublisher,
            pressureProjectionService,
            timeProvider);
        var command = new DisburseCityBudgetToBusinessCommand(
            CityId: cityId,
            BusinessId: business.Id,
            Category: CityBudgetCategory.Infrastructure,
            Amount: 90m,
            Title: "Bridge Contractor Payment",
            Description: "Weekly milestone");

        var result = await handler.Handle(command, CancellationToken.None);

        CityBudgetLedgerEntry budgetEntry = Assert.Single(budgetLedgerRepository.AddedEntries);
        CityBusinessLedgerEntry businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
        var signal = Assert.Single(signalPublisher.PublishedSignals);
        Assert.Equal(timeProvider.UtcNow, budgetEntry.OccurredAtUtc);
        Assert.Equal(timeProvider.UtcNow, businessEntry.OccurredAtUtc);
        Assert.Equal(610m, budget.Balance.Amount);
        Assert.Equal(90m, budget.TotalCityExpenses.Amount);
        Assert.Equal(240m, business.Balance.Amount);
        Assert.Equal(115m, allocation.TotalSpent.Amount);
        Assert.Equal(timeProvider.UtcNow, allocation.UpdatedAtUtc);
        Assert.Equal(cityId, pressureProjectionService.RequestedCityId);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, signal.EffectiveAtUtc);
        Assert.Equal(timeProvider.UtcNow, signal.OccurredAtUtc);
        Assert.Equal("Expense", result.Kind);
        Assert.Equal("MunicipalDisbursement", result.Source);
        Assert.Equal(90m, result.Amount);
        Assert.Equal(business.Id.ToString("N"), result.ReferenceCode);
    }

    [Fact]
    public async Task Handle_ThrowsWhenBusinessBelongsToAnotherCity()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBusiness business = CreateBusiness(
            Guid.Parse("ffffffff-1111-2222-3333-444444444444"),
            "Outsider Vendor",
            CityBusinessKind.MunicipalVendor,
            50m);
        var businessRepository = new FakeCityBusinessRepository { Businesses = [business] };
        var handler = new DisburseCityBudgetToBusinessCommandHandler(
            businessRepository,
            new CityBudgetBusinessDisbursementSupport(
                new FakeCityBudgetRepository(),
                new FakeCityBudgetLedgerRepository(),
                new FakeCityBusinessLedgerRepository(),
                new CityBudgetAllocationExpenseSupport(
                    new FakeCityBudgetAllocationRepository(),
                    new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 17, 40, 0, TimeSpan.Zero))),
                new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 17, 40, 0, TimeSpan.Zero))),
            new FakeEconomyUnitOfWork(),
            new FakeCityOperationalBudgetSignalPublisher(),
            new FakeCityOperationalBudgetPressureProjectionService(),
            new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 17, 40, 0, TimeSpan.Zero)));
        var command = new DisburseCityBudgetToBusinessCommand(
            CityId: cityId,
            BusinessId: business.Id,
            Category: CityBudgetCategory.General,
            Amount: 25m,
            Title: "Invalid Transfer",
            Description: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Business and budget must belong to the same city.", exception.Message);
    }
}
