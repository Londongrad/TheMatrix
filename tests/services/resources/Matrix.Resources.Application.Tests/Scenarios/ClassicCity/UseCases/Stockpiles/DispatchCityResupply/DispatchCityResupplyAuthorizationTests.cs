using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;

public sealed class DispatchCityResupplyAuthorizationTests
{
    [Fact]
    public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
    {
        var handler = new DispatchCityResupplyCommandHandler(
            new FakeCityStockpileRepository(),
            new FakeUnitOfWork(),
            new FakeCityOperationalExpenseOutboxWriter(),
            new FakeCityBudgetAuthorizationClient(),
            new CityStockpileBudgetGuard(),
            new FakeCityResupplyTripDispatcher(),
            CreateTimeProvider());

        DispatchCityResupplyResult result = await handler.Handle(
            new DispatchCityResupplyCommand(CityId, ResupplyFocus.All, ResupplyIntensity.High, false),
            CancellationToken.None);

        Assert.Equal(DispatchCityResupplyStatus.NotInitialized, result.Status);
        Assert.Equal("Unavailable", result.BudgetAuthorizationStatus);
    }

    [Fact]
    public async Task Handler_ReturnsAuthorizationDeniedWhenBudgetClientRejectsExplicitApproval()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        var authorizationClient = new FakeCityBudgetAuthorizationClient
        {
            Response = new CityBudgetAuthorizationDecision(
                Status: "Denied",
                RequestedIntensity: "High",
                ApprovedIntensity: null,
                AuthorizationLevel: "Low",
                AvailableAmount: 120m,
                PressureIndex: 0.72m,
                EmergencyOverrideRequested: false,
                AuthorizedByEmergencyOverride: false,
                Summary: "Budget pressure does not allow this dispatch.")
        };
        var unitOfWork = new FakeUnitOfWork();
        var expenseOutboxWriter = new FakeCityOperationalExpenseOutboxWriter();
        var handler = new DispatchCityResupplyCommandHandler(
            repository,
            unitOfWork,
            expenseOutboxWriter,
            authorizationClient,
            new CityStockpileBudgetGuard(),
            new FakeCityResupplyTripDispatcher(),
            CreateTimeProvider());

        DispatchCityResupplyResult result = await handler.Handle(
            new DispatchCityResupplyCommand(CityId, ResupplyFocus.All, ResupplyIntensity.High, false),
            CancellationToken.None);

        Assert.Equal(DispatchCityResupplyStatus.AuthorizationDenied, result.Status);
        Assert.Equal(1, authorizationClient.CallCount);
        Assert.Null(result.PendingResupply);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(expenseOutboxWriter.Expenses);
    }

    [Fact]
    public async Task Handler_ReturnsBudgetBlockedWhenSnapshotCapsDisallowDispatch()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.ApplyOperationalBudgetPressure(new CityOperationalBudgetPressureSnapshot(
            Balance: 10_000m,
            MunicipalOperationsExpenses: 8_500m,
            GeneralAvailableAmount: 250m,
            OperationsAvailableAmount: 200m,
            InfrastructureAvailableAmount: 180m,
            HealthcareAvailableAmount: 160m,
            GeneralAuthorizationLevel: "None",
            OperationsAuthorizationLevel: "Low",
            InfrastructureAuthorizationLevel: "Low",
            HealthcareAuthorizationLevel: "Low",
            PressureIndex: 0.88m,
            EffectiveTickId: 4,
            EffectiveAtUtc: LaterUtc));
        var authorizationClient = new FakeCityBudgetAuthorizationClient();
        var unitOfWork = new FakeUnitOfWork();
        var expenseOutboxWriter = new FakeCityOperationalExpenseOutboxWriter();
        var handler = new DispatchCityResupplyCommandHandler(
            repository,
            unitOfWork,
            expenseOutboxWriter,
            authorizationClient,
            new CityStockpileBudgetGuard(),
            new FakeCityResupplyTripDispatcher(),
            CreateTimeProvider());

        DispatchCityResupplyResult result = await handler.Handle(
            new DispatchCityResupplyCommand(CityId, ResupplyFocus.Food, ResupplyIntensity.Low, false),
            CancellationToken.None);

        Assert.Equal(DispatchCityResupplyStatus.BudgetBlocked, result.Status);
        Assert.Equal("NotRequired", result.BudgetAuthorizationStatus);
        Assert.Equal(0, authorizationClient.CallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(expenseOutboxWriter.Expenses);
    }
}
