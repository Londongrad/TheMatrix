using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<SyncCityOperationalBudgetPressureCommand, SyncCityOperationalBudgetPressureResult>
    {
        public async Task<SyncCityOperationalBudgetPressureResult> Handle(
            SyncCityOperationalBudgetPressureCommand request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
            {
                return new SyncCityOperationalBudgetPressureResult(
                    Status: SyncCityOperationalBudgetPressureStatus.NotInitialized,
                    PressureIndex: 0m,
                    EffectiveTickId: request.EffectiveTickId,
                    EffectiveAtUtc: request.EffectiveAtUtc);
            }

            if (IsIncomingSnapshotStale(
                    effectiveTickId: request.EffectiveTickId,
                    effectiveAtUtc: request.EffectiveAtUtc,
                    currentEffectiveTickId: state.OperationalBudgetPressure.EffectiveTickId,
                    currentEffectiveAtUtc: state.OperationalBudgetPressure.EffectiveAtUtc))
            {
                return new SyncCityOperationalBudgetPressureResult(
                    Status: SyncCityOperationalBudgetPressureStatus.Stale,
                    PressureIndex: state.OperationalBudgetPressure.PressureIndex,
                    EffectiveTickId: state.OperationalBudgetPressure.EffectiveTickId,
                    EffectiveAtUtc: state.OperationalBudgetPressure.EffectiveAtUtc);
            }

            state.ApplyOperationalBudgetPressure(
                snapshot: new CityOperationalBudgetPressureSnapshot(
                    Balance: request.Balance,
                    MunicipalOperationsExpenses: request.MunicipalOperationsExpenses,
                    GeneralAvailableAmount: request.GeneralAvailableAmount,
                    OperationsAvailableAmount: request.OperationsAvailableAmount,
                    InfrastructureAvailableAmount: request.InfrastructureAvailableAmount,
                    HealthcareAvailableAmount: request.HealthcareAvailableAmount,
                    GeneralAuthorizationLevel: request.GeneralAuthorizationLevel,
                    OperationsAuthorizationLevel: request.OperationsAuthorizationLevel,
                    InfrastructureAuthorizationLevel: request.InfrastructureAuthorizationLevel,
                    HealthcareAuthorizationLevel: request.HealthcareAuthorizationLevel,
                    PressureIndex: request.PressureIndex,
                    EffectiveTickId: request.EffectiveTickId,
                    EffectiveAtUtc: request.EffectiveAtUtc));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (IsConcurrencyException(exception))
            {
                CityEnvironmentalConditionState? persistedState = await repository.GetBySimulationHostIdNoTrackingAsync(
                    simulationHostId: simulationHostId,
                    cancellationToken: cancellationToken);

                if (persistedState is null)
                {
                    return new SyncCityOperationalBudgetPressureResult(
                        Status: SyncCityOperationalBudgetPressureStatus.NotInitialized,
                        PressureIndex: 0m,
                        EffectiveTickId: request.EffectiveTickId,
                        EffectiveAtUtc: request.EffectiveAtUtc);
                }

                if (IsIncomingSnapshotStale(
                        effectiveTickId: request.EffectiveTickId,
                        effectiveAtUtc: request.EffectiveAtUtc,
                        currentEffectiveTickId: persistedState.OperationalBudgetPressure.EffectiveTickId,
                        currentEffectiveAtUtc: persistedState.OperationalBudgetPressure.EffectiveAtUtc))
                {
                    return new SyncCityOperationalBudgetPressureResult(
                        Status: SyncCityOperationalBudgetPressureStatus.Stale,
                        PressureIndex: persistedState.OperationalBudgetPressure.PressureIndex,
                        EffectiveTickId: persistedState.OperationalBudgetPressure.EffectiveTickId,
                        EffectiveAtUtc: persistedState.OperationalBudgetPressure.EffectiveAtUtc);
                }

                if (MatchesSnapshot(
                        request: request,
                        state: persistedState.OperationalBudgetPressure))
                {
                    return new SyncCityOperationalBudgetPressureResult(
                        Status: SyncCityOperationalBudgetPressureStatus.Concurrent,
                        PressureIndex: persistedState.OperationalBudgetPressure.PressureIndex,
                        EffectiveTickId: persistedState.OperationalBudgetPressure.EffectiveTickId,
                        EffectiveAtUtc: persistedState.OperationalBudgetPressure.EffectiveAtUtc);
                }

                throw;
            }

            return new SyncCityOperationalBudgetPressureResult(
                Status: SyncCityOperationalBudgetPressureStatus.Applied,
                PressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EffectiveTickId: state.OperationalBudgetPressure.EffectiveTickId,
                EffectiveAtUtc: state.OperationalBudgetPressure.EffectiveAtUtc);
        }

        private static bool IsIncomingSnapshotStale(
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            long currentEffectiveTickId,
            DateTimeOffset currentEffectiveAtUtc)
        {
            if (effectiveTickId < currentEffectiveTickId)
                return true;

            if (effectiveTickId > currentEffectiveTickId)
                return false;

            return effectiveAtUtc < currentEffectiveAtUtc;
        }

        private static bool MatchesSnapshot(
            SyncCityOperationalBudgetPressureCommand request,
            CityOperationalBudgetPressureState state)
        {
            return state.EffectiveTickId == request.EffectiveTickId &&
                   state.EffectiveAtUtc == request.EffectiveAtUtc &&
                   state.Balance == request.Balance &&
                   state.MunicipalOperationsExpenses == request.MunicipalOperationsExpenses &&
                   state.GeneralAvailableAmount == request.GeneralAvailableAmount &&
                   state.OperationsAvailableAmount == request.OperationsAvailableAmount &&
                   state.InfrastructureAvailableAmount == request.InfrastructureAvailableAmount &&
                   state.HealthcareAvailableAmount == request.HealthcareAvailableAmount &&
                   string.Equals(state.GeneralAuthorizationLevel, request.GeneralAuthorizationLevel, StringComparison.Ordinal) &&
                   string.Equals(state.OperationsAuthorizationLevel, request.OperationsAuthorizationLevel, StringComparison.Ordinal) &&
                   string.Equals(state.InfrastructureAuthorizationLevel, request.InfrastructureAuthorizationLevel, StringComparison.Ordinal) &&
                   string.Equals(state.HealthcareAuthorizationLevel, request.HealthcareAuthorizationLevel, StringComparison.Ordinal) &&
                   state.PressureIndex == request.PressureIndex;
        }

        private static bool IsConcurrencyException(Exception exception)
        {
            return exception.GetType().Name == "DbUpdateConcurrencyException";
        }
    }
}
