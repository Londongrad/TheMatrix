using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;

public sealed class DispatchCityResupplySchedulingTests
{
    [Fact]
    public async Task Handler_SchedulesResupplyWithoutExplicitAuthorizationWhenBudgetCheckIsLocal()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        var unitOfWork = new FakeUnitOfWork();
        var expenseOutboxWriter = new FakeCityOperationalExpenseOutboxWriter();
        var authorizationClient = new FakeCityBudgetAuthorizationClient();
        var tripDispatcher = new FakeCityResupplyTripDispatcher();
        DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(45);
        var handler = new DispatchCityResupplyCommandHandler(
            repository,
            unitOfWork,
            expenseOutboxWriter,
            authorizationClient,
            new CityStockpileBudgetGuard(),
            tripDispatcher,
            CreateTimeProvider(occurredAtUtc));

        DispatchCityResupplyResult result = await handler.Handle(
            new DispatchCityResupplyCommand(CityId, ResupplyFocus.Food, ResupplyIntensity.Low, false),
            CancellationToken.None);

        Assert.Equal(DispatchCityResupplyStatus.Scheduled, result.Status);
        Assert.Equal("NotRequired", result.BudgetAuthorizationStatus);
        Assert.Equal(0, authorizationClient.CallCount);
        Assert.NotNull(result.PendingResupply);
        Assert.Equal("Food", result.PendingResupply!.Focus);
        Assert.Equal("Low", result.PendingResupply.Intensity);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(expenseOutboxWriter.Expenses);
        Assert.Equal(occurredAtUtc, expenseOutboxWriter.Expenses[0].OccurredAtUtc);
        Assert.Equal(0, tripDispatcher.CallCount);
    }

    [Fact]
    public async Task Handler_SchedulesDistrictResupplyWithApprovedIntensityAndDispatchesTrip()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        var unitOfWork = new FakeUnitOfWork();
        var expenseOutboxWriter = new FakeCityOperationalExpenseOutboxWriter();
        var authorizationClient = new FakeCityBudgetAuthorizationClient
        {
            Response = new CityBudgetAuthorizationDecision(
                Status: "Approved",
                RequestedIntensity: "High",
                ApprovedIntensity: "Medium",
                AuthorizationLevel: "Medium",
                AvailableAmount: 480m,
                PressureIndex: 0.41m,
                EmergencyOverrideRequested: false,
                AuthorizedByEmergencyOverride: false,
                Summary: "Dispatch approved with reduced intensity.")
        };
        var tripDispatcher = new FakeCityResupplyTripDispatcher();
        Guid districtId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(50);
        var handler = new DispatchCityResupplyCommandHandler(
            repository,
            unitOfWork,
            expenseOutboxWriter,
            authorizationClient,
            new CityStockpileBudgetGuard(),
            tripDispatcher,
            CreateTimeProvider(occurredAtUtc));

        DispatchCityResupplyResult result = await handler.Handle(
            new DispatchCityResupplyCommand(CityId, ResupplyFocus.Fuel, ResupplyIntensity.High, false, districtId),
            CancellationToken.None);

        Assert.Equal(DispatchCityResupplyStatus.Scheduled, result.Status);
        Assert.Equal("Medium", result.BudgetAuthorizedIntensity);
        Assert.NotNull(result.PendingResupply);
        Assert.Equal("Medium", result.PendingResupply!.Intensity);
        Assert.Equal(1, authorizationClient.CallCount);
        Assert.Equal(CityId, authorizationClient.Request!.CityId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(expenseOutboxWriter.Expenses);
        Assert.Equal(occurredAtUtc, expenseOutboxWriter.Expenses[0].OccurredAtUtc);
        Assert.Equal(1, tripDispatcher.CallCount);
        Assert.Equal(districtId, tripDispatcher.FocusDistrictId);
        Assert.Equal("Fuel", tripDispatcher.Focus);
        Assert.Equal("Medium", tripDispatcher.Intensity);
    }
}
