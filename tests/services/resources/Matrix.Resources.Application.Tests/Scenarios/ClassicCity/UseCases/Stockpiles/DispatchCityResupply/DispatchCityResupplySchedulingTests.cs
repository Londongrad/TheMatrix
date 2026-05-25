using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
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
                repository: repository,
                unitOfWork: unitOfWork,
                expenseOutboxWriter: expenseOutboxWriter,
                budgetAuthorizationClient: authorizationClient,
                budgetGuard: new CityStockpileBudgetGuard(),
                resupplyTripDispatcher: tripDispatcher,
                timeProvider: CreateTimeProvider(occurredAtUtc));

            DispatchCityResupplyResult result = await handler.Handle(
                request: new DispatchCityResupplyCommand(
                    CityId: CityId,
                    Focus: ResupplyFocus.Food,
                    Intensity: ResupplyIntensity.Low,
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityResupplyStatus.Scheduled,
                actual: result.Status);
            Assert.Equal(
                expected: "NotRequired",
                actual: result.BudgetAuthorizationStatus);
            Assert.Equal(
                expected: 0,
                actual: authorizationClient.CallCount);
            Assert.NotNull(result.PendingResupply);
            Assert.Equal(
                expected: "Food",
                actual: result.PendingResupply!.Focus);
            Assert.Equal(
                expected: "Low",
                actual: result.PendingResupply.Intensity);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Single(expenseOutboxWriter.Expenses);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: expenseOutboxWriter.Expenses[0].OccurredAtUtc);
            Assert.Equal(
                expected: 0,
                actual: tripDispatcher.CallCount);
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
            var districtId = Guid.Parse("60000000-0000-0000-0000-000000000001");
            DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(50);
            var handler = new DispatchCityResupplyCommandHandler(
                repository: repository,
                unitOfWork: unitOfWork,
                expenseOutboxWriter: expenseOutboxWriter,
                budgetAuthorizationClient: authorizationClient,
                budgetGuard: new CityStockpileBudgetGuard(),
                resupplyTripDispatcher: tripDispatcher,
                timeProvider: CreateTimeProvider(occurredAtUtc));

            DispatchCityResupplyResult result = await handler.Handle(
                request: new DispatchCityResupplyCommand(
                    CityId: CityId,
                    Focus: ResupplyFocus.Fuel,
                    Intensity: ResupplyIntensity.High,
                    EmergencyOverride: false,
                    FocusDistrictId: districtId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityResupplyStatus.Scheduled,
                actual: result.Status);
            Assert.Equal(
                expected: "Medium",
                actual: result.BudgetAuthorizedIntensity);
            Assert.NotNull(result.PendingResupply);
            Assert.Equal(
                expected: "Medium",
                actual: result.PendingResupply!.Intensity);
            Assert.Equal(
                expected: 1,
                actual: authorizationClient.CallCount);
            Assert.Equal(
                expected: CityId,
                actual: authorizationClient.Request!.CityId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Single(expenseOutboxWriter.Expenses);
            Assert.Equal(
                expected: occurredAtUtc,
                actual: expenseOutboxWriter.Expenses[0].OccurredAtUtc);
            Assert.Equal(
                expected: 1,
                actual: tripDispatcher.CallCount);
            Assert.Equal(
                expected: districtId,
                actual: tripDispatcher.FocusDistrictId);
            Assert.Equal(
                expected: "Fuel",
                actual: tripDispatcher.Focus);
            Assert.Equal(
                expected: "Medium",
                actual: tripDispatcher.Intensity);
        }
    }
}
