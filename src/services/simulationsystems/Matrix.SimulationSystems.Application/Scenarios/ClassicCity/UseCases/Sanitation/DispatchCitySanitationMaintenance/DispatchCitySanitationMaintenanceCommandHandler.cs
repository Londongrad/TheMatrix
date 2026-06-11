using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.
    DispatchCitySanitationMaintenance
{
    public sealed class DispatchCitySanitationMaintenanceCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityOperationalExpenseOutboxWriter operationalExpenseOutboxWriter,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory,
        CityMaintenanceBudgetGuard budgetGuard,
        CityMaintenanceBudgetAuthorizationService budgetAuthorizationService,
        TimeProvider timeProvider)
        : IRequestHandler<DispatchCitySanitationMaintenanceCommand, CitySanitationStatusDto?>
    {
        public async Task<CitySanitationStatusDto?> Handle(
            DispatchCitySanitationMaintenanceCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            SanitationMaintenanceFocus focus = Enum.Parse<SanitationMaintenanceFocus>(
                value: request.Focus,
                ignoreCase: true);
            SanitationMaintenanceIntensity requestedIntensity = Enum.Parse<SanitationMaintenanceIntensity>(
                value: request.Intensity,
                ignoreCase: true);
            CityBudgetAuthorizationDecision authorizationDecision =
                await budgetAuthorizationService.AuthorizeInfrastructureMaintenanceAsync(
                    cityId: request.CityId,
                    operationKind: "SanitationMaintenanceDispatch",
                    requestedIntensity: request.Intensity,
                    estimatedAmount: CityMaintenanceOperationalExpenseFactory.EstimateInfrastructureMaintenanceAmount(
                        systemName: "Sanitation",
                        focus: request.Focus,
                        intensity: request.Intensity),
                    emergencyOverrideRequested: request.EmergencyOverride,
                    emergencyModeEnabled: state.SanitationInfrastructure.EmergencyModeEnabled,
                    defaultAuthorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                    defaultAvailableAmount: state.OperationalBudgetPressure.InfrastructureAvailableAmount,
                    pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                    cancellationToken: cancellationToken);

            if (authorizationDecision.Denied)
            {
                decimal deniedSupport = pressureProfileFactory.Create(state)
                   .SanitationSupport;

                return CitySanitationStatusDto.FromState(
                    cityId: request.CityId,
                    state: state,
                    sanitationSupportIndex: deniedSupport,
                    requestedIntensity: request.Intensity,
                    appliedIntensity: null,
                    budgetAuthorizationStatus: authorizationDecision.Status,
                    budgetAuthorizationLevel: authorizationDecision.AuthorizationLevel,
                    budgetAvailableAmount: authorizationDecision.AvailableAmount,
                    budgetAuthorizedByEmergencyOverride: authorizationDecision.AuthorizedByEmergencyOverride,
                    budgetAuthorizedIntensity: authorizationDecision.ApprovedIntensity,
                    budgetAuthorizationSummary: authorizationDecision.Summary);
            }

            SanitationMaintenanceIntensity budgetAuthorizedIntensity = Enum.Parse<SanitationMaintenanceIntensity>(
                value: authorizationDecision.ApprovedIntensity ?? requestedIntensity.ToString(),
                ignoreCase: true);
            CityMaintenanceBudgetDecision budgetDecision = budgetGuard.Resolve(
                requestedIntensity: budgetAuthorizedIntensity.ToString(),
                authorizationLevel: state.OperationalBudgetPressure.InfrastructureAuthorizationLevel,
                pressureIndex: state.OperationalBudgetPressure.PressureIndex,
                emergencyModeEnabled: state.SanitationInfrastructure.EmergencyModeEnabled || request.EmergencyOverride);
            SanitationMaintenanceIntensity appliedIntensity = Enum.Parse<SanitationMaintenanceIntensity>(
                value: budgetDecision.AppliedIntensity,
                ignoreCase: true);

            state.ScheduleSanitationMaintenance(
                focus: focus,
                intensity: appliedIntensity,
                readyAtTickId: CalculateReadyAtTickId(
                    currentTickId: state.LastAppliedTickId,
                    intensity: budgetDecision.AppliedIntensity));
            await operationalExpenseOutboxWriter.AddClassicCityOperationalExpenseAsync(
                expense: CityMaintenanceOperationalExpenseFactory.CreateInfrastructureMaintenanceExpense(
                    cityId: request.CityId,
                    systemName: "Sanitation",
                    operationKind: "SanitationMaintenanceDispatch",
                    focus: request.Focus,
                    intensity: budgetDecision.AppliedIntensity,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal sanitationSupport = pressureProfileFactory.Create(state)
               .SanitationSupport;

            return CitySanitationStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                sanitationSupportIndex: sanitationSupport,
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
