using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
    public sealed class DispatchCityResupplyCommandHandler(
        ICityStockpileRepository repository,
        IUnitOfWork unitOfWork,
        ICityStockpileSnapshotOutboxWriter outboxWriter,
        ICityOperationalExpenseOutboxWriter expenseOutboxWriter,
        ICityBudgetAuthorizationClient budgetAuthorizationClient,
        CityStockpileBudgetGuard budgetGuard,
        CityStockpilePolicy policy)
        : IRequestHandler<DispatchCityResupplyCommand, DispatchCityResupplyResult>
    {
        public async Task<DispatchCityResupplyResult> Handle(
            DispatchCityResupplyCommand request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);

            var state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return new DispatchCityResupplyResult(
                    Status: DispatchCityResupplyStatus.NotInitialized,
                    CityId: request.CityId,
                    RequestedIntensity: request.Intensity.ToString(),
                    BudgetAuthorizedIntensity: null,
                    AppliedIntensity: null,
                    BudgetPressureIndex: 0m,
                    BudgetAuthorizationStatus: "Unavailable",
                    BudgetAuthorizationLevel: "High",
                    BudgetAvailableAmount: 0m,
                    BudgetAuthorizedByEmergencyOverride: false,
                    BudgetAuthorizationSummary: "City stockpiles are not initialized yet.",
                    SupplyStressIndex: 0m,
                    FuelStockLevelIndex: 0m,
                    FoodStockLevelIndex: 0m,
                    EmergencyWaterStockLevelIndex: 0m);

            CityBudgetAuthorizationDecision authorizationDecision = CityBudgetAuthorizationDecision.NotRequired(
                requestedIntensity: request.Intensity.ToString(),
                pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                authorizationLevel: state.OperationalBudgetPressure.OperationsAuthorizationLevel,
                availableAmount: state.OperationalBudgetPressure.OperationsAvailableAmount);

            if (RequiresExplicitAuthorization(request))
            {
                authorizationDecision = await budgetAuthorizationClient.AuthorizeAsync(
                    request: new CityBudgetAuthorizationRequest(
                        CityId: request.CityId,
                        Category: CityResupplyOperationalExpenseFactory.ResolveBudgetCategory(request.Focus),
                        OperationKind: "StockpileResupplyDispatch",
                        RequestedIntensity: request.Intensity.ToString(),
                        EstimatedAmount: CityResupplyOperationalExpenseFactory.EstimateDispatchAmount(
                            focus: request.Focus,
                            intensity: request.Intensity),
                        EmergencyOverrideRequested: request.EmergencyOverride),
                    cancellationToken: cancellationToken);

                if (authorizationDecision.Denied)
                    return new DispatchCityResupplyResult(
                        Status: DispatchCityResupplyStatus.AuthorizationDenied,
                        CityId: request.CityId,
                        RequestedIntensity: authorizationDecision.RequestedIntensity,
                        BudgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                        AppliedIntensity: null,
                        BudgetPressureIndex: authorizationDecision.PressureIndex,
                        BudgetAuthorizationStatus: authorizationDecision.Status,
                        BudgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                        BudgetAvailableAmount: authorizationDecision.AvailableAmount,
                        BudgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                        BudgetAuthorizationSummary: authorizationDecision.Summary,
                        SupplyStressIndex: state.SupplyStressIndex,
                        FuelStockLevelIndex: state.Fuel.StockLevelIndex,
                        FoodStockLevelIndex: state.Food.StockLevelIndex,
                        EmergencyWaterStockLevelIndex: state.EmergencyWater.StockLevelIndex);
            }

            ResupplyIntensity budgetAuthorizedIntensity = Enum.Parse<ResupplyIntensity>(
                value: authorizationDecision.ApprovedIntensity ?? request.Intensity.ToString(),
                ignoreCase: true);
            CityStockpileBudgetDecision decision = budgetGuard.ResolveResupply(
                focus: request.Focus,
                requestedIntensity: budgetAuthorizedIntensity,
                budget: state.OperationalBudgetPressure.ToSnapshot(),
                emergencyRationingEnabled: state.EmergencyRationingEnabled,
                emergencyOverrideRequested: request.EmergencyOverride);

            if (decision.Blocked)
                return new DispatchCityResupplyResult(
                    Status: DispatchCityResupplyStatus.BudgetBlocked,
                    CityId: request.CityId,
                    RequestedIntensity: request.Intensity.ToString(),
                    BudgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                    AppliedIntensity: decision.AppliedIntensity.ToString(),
                    BudgetPressureIndex: decision.PressureIndex,
                    BudgetAuthorizationStatus: authorizationDecision.Status,
                    BudgetAuthorizationLevel: authorizationDecision.Status == "NotRequired"
                        ? decision.AuthorizationLevel
                        : authorizationDecision.AuthorizationLevel,
                    BudgetAvailableAmount: authorizationDecision.Status == "NotRequired"
                        ? decision.AvailableAmount
                        : authorizationDecision.AvailableAmount,
                    BudgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                    BudgetAuthorizationSummary: authorizationDecision.Status == "NotRequired"
                        ? "Snapshot budget caps blocked the requested resupply dispatch."
                        : authorizationDecision.Summary,
                    SupplyStressIndex: state.SupplyStressIndex,
                    FuelStockLevelIndex: state.Fuel.StockLevelIndex,
                    FoodStockLevelIndex: state.Food.StockLevelIndex,
                    EmergencyWaterStockLevelIndex: state.EmergencyWater.StockLevelIndex);

            var refreshedSnapshot = policy.DispatchResupply(
                current: state.ToSnapshot(),
                focus: request.Focus,
                intensity: decision.AppliedIntensity);

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;

            state.ApplySnapshot(refreshedSnapshot);
            await outboxWriter.AddClassicCityStockpileSnapshotAsync(
                snapshot: CityStockpileIntegrationEventFactory.CreateSnapshot(
                    state: state,
                    occurredAtUtc: occurredAtUtc),
                cancellationToken: cancellationToken);
            await expenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityResupplyOperationalExpenseFactory.CreateDispatchExpense(
                    cityId: request.CityId,
                    focus: request.Focus,
                    intensity: decision.AppliedIntensity,
                    occurredAtUtc: occurredAtUtc),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DispatchCityResupplyResult(
                Status: DispatchCityResupplyStatus.Applied,
                CityId: request.CityId,
                RequestedIntensity: request.Intensity.ToString(),
                BudgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                AppliedIntensity: decision.AppliedIntensity.ToString(),
                BudgetPressureIndex: decision.PressureIndex,
                BudgetAuthorizationStatus: authorizationDecision.Status,
                BudgetAuthorizationLevel: authorizationDecision.Status == "NotRequired"
                    ? decision.AuthorizationLevel
                    : authorizationDecision.AuthorizationLevel,
                BudgetAvailableAmount: authorizationDecision.Status == "NotRequired"
                    ? decision.AvailableAmount
                    : authorizationDecision.AvailableAmount,
                BudgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                BudgetAuthorizationSummary: authorizationDecision.Summary,
                SupplyStressIndex: state.SupplyStressIndex,
                FuelStockLevelIndex: state.Fuel.StockLevelIndex,
                FoodStockLevelIndex: state.Food.StockLevelIndex,
                EmergencyWaterStockLevelIndex: state.EmergencyWater.StockLevelIndex);
        }

        private static bool RequiresExplicitAuthorization(DispatchCityResupplyCommand request)
        {
            return request.EmergencyOverride ||
                   request.Focus == ResupplyFocus.All ||
                   request.Intensity == ResupplyIntensity.High;
        }
    }
}
