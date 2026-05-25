using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
    public sealed class DispatchCityResupplyAuthorizationTests
    {
        [Fact]
        public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
        {
            var handler = new DispatchCityResupplyCommandHandler(
                repository: new FakeCityStockpileRepository(),
                unitOfWork: new FakeUnitOfWork(),
                expenseOutboxWriter: new FakeCityOperationalExpenseOutboxWriter(),
                budgetAuthorizationClient: new FakeCityBudgetAuthorizationClient(),
                budgetGuard: new CityStockpileBudgetGuard(),
                resupplyTripDispatcher: new FakeCityResupplyTripDispatcher(),
                timeProvider: CreateTimeProvider());

            DispatchCityResupplyResult result = await handler.Handle(
                request: new DispatchCityResupplyCommand(
                    CityId: CityId,
                    Focus: ResupplyFocus.All,
                    Intensity: ResupplyIntensity.High,
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityResupplyStatus.NotInitialized,
                actual: result.Status);
            Assert.Equal(
                expected: "Unavailable",
                actual: result.BudgetAuthorizationStatus);
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
                repository: repository,
                unitOfWork: unitOfWork,
                expenseOutboxWriter: expenseOutboxWriter,
                budgetAuthorizationClient: authorizationClient,
                budgetGuard: new CityStockpileBudgetGuard(),
                resupplyTripDispatcher: new FakeCityResupplyTripDispatcher(),
                timeProvider: CreateTimeProvider());

            DispatchCityResupplyResult result = await handler.Handle(
                request: new DispatchCityResupplyCommand(
                    CityId: CityId,
                    Focus: ResupplyFocus.All,
                    Intensity: ResupplyIntensity.High,
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityResupplyStatus.AuthorizationDenied,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: authorizationClient.CallCount);
            Assert.Null(result.PendingResupply);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Empty(expenseOutboxWriter.Expenses);
        }

        [Fact]
        public async Task Handler_ReturnsBudgetBlockedWhenSnapshotCapsDisallowDispatch()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.ApplyOperationalBudgetPressure(
                new CityOperationalBudgetPressureSnapshot(
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
                repository: repository,
                unitOfWork: unitOfWork,
                expenseOutboxWriter: expenseOutboxWriter,
                budgetAuthorizationClient: authorizationClient,
                budgetGuard: new CityStockpileBudgetGuard(),
                resupplyTripDispatcher: new FakeCityResupplyTripDispatcher(),
                timeProvider: CreateTimeProvider());

            DispatchCityResupplyResult result = await handler.Handle(
                request: new DispatchCityResupplyCommand(
                    CityId: CityId,
                    Focus: ResupplyFocus.Food,
                    Intensity: ResupplyIntensity.Low,
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityResupplyStatus.BudgetBlocked,
                actual: result.Status);
            Assert.Equal(
                expected: "NotRequired",
                actual: result.BudgetAuthorizationStatus);
            Assert.Equal(
                expected: 0,
                actual: authorizationClient.CallCount);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Empty(expenseOutboxWriter.Expenses);
        }
    }
}
