using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance
{
    public sealed class DispatchCityDrainageMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard,
        CityMaintenanceBudgetAuthorizationService budgetAuthorizationService,
        TimeProvider timeProvider)
        : IRequestHandler<DispatchCityDrainageMaintenanceCommand, CityDrainageStatusDto?>
    {
        public async Task<CityDrainageStatusDto?> Handle(
            DispatchCityDrainageMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            DrainageMaintenanceFocus focus = Enum.Parse<DrainageMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            DrainageMaintenanceIntensity requestedIntensity = Enum.Parse<DrainageMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);
            CityBudgetAuthorizationDecision authorizationDecision =
                await budgetAuthorizationService.AuthorizeInfrastructureMaintenanceAsync(
                    cityId: request.CityId,
                    operationKind: "DrainageMaintenanceDispatch",
                    requestedIntensity: request.Intensity,
                    estimatedAmount: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                        systemName: "Drainage",
                        focus: request.Focus,
                        intensity: request.Intensity),
                    emergencyOverrideRequested: request.EmergencyOverride,
                    emergencyModeEnabled: state.DrainageInfrastructure.EmergencyModeEnabled,
                    defaultAuthorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                    defaultAvailableAmount: state.OperationalBudgetPressure.InfrastructureAvailableAmount,
                    pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                    cancellationToken: cancellationToken);

            if (authorizationDecision.Denied)
            {
                decimal deniedSupport = pressureProfileFactory.Create(state)
                   .DrainageSupport;

                return CityDrainageStatusDto.FromState(
                    cityId: request.CityId,
                    state: state,
                    drainageSupportIndex: deniedSupport,
                    requestedIntensity: request.Intensity,
                    appliedIntensity: null,
                    budgetAuthorizationStatus: authorizationDecision.Status,
                    budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                    budgetAvailableAmount: authorizationDecision.AvailableAmount,
                    budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                    budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                    budgetAuthorizationSummary: authorizationDecision.Summary);
            }

            DrainageMaintenanceIntensity budgetAuthorizedIntensity = Enum.Parse<DrainageMaintenanceIntensity>(
                value: authorizationDecision.ApprovedIntensity ?? requestedIntensity.ToString(),
                ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: budgetAuthorizedIntensity.ToString(),
                authorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                emergencyModeEnabled: state.DrainageInfrastructure.EmergencyModeEnabled || request.EmergencyOverride);
            DrainageMaintenanceIntensity appliedIntensity = Enum.Parse<DrainageMaintenanceIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.ScheduleDrainageMaintenance(
                focus: focus,
                intensity: appliedIntensity,
                readyAtTickId: CalculateReadyAtTickId(
                    currentTickId: state.LastAppliedTickId,
                    intensity: budgetDecision.AppliedIntensity));
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateInfrastructureMaintenanceExpense(
                    cityId: request.CityId,
                    systemName: "Drainage",
                    operationKind: "DrainageMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal drainageSupport = pressureProfileFactory.Create(state)
               .DrainageSupport;

            return CityDrainageStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                drainageSupportIndex: drainageSupport,
                requestedIntensity: request.Intensity,
                appliedIntensity: appliedIntensity.ToString(),
                budgetAuthorizationStatus: authorizationDecision.Status,
                budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                budgetAvailableAmount: authorizationDecision.AvailableAmount,
                budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                budgetAuthorizationSummary: authorizationDecision.Summary);
        }

        private static long CalculateReadyAtTickId(
            long currentTickId,
            string intensity)
        {
            long delay = string.Equals(
                a: intensity,
                b: "Heavy",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? 2
                : 1;

            return Math.Max(
                val1: 0,
                val2: currentTickId + delay);
        }
    }
}
