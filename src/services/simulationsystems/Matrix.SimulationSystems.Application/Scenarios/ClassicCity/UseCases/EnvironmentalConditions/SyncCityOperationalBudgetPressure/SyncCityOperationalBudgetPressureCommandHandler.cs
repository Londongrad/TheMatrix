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
        private const int MaxSaveAttempts = 3;

        public async Task<SyncCityOperationalBudgetPressureResult> Handle(
            SyncCityOperationalBudgetPressureCommand request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);
            CityOperationalBudgetPressureSnapshot snapshot = new(
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
                EffectiveAtUtc: request.EffectiveAtUtc);

            for (int attempt = 0; attempt < MaxSaveAttempts; attempt++)
            {
                CityEnvironmentalConditionState? state = attempt == 0
                    ? await repository.GetBySimulationHostIdAsync(
                        simulationHostId: simulationHostId,
                        cancellationToken: cancellationToken)
                    : await repository.GetFreshBySimulationHostIdAsync(
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
                    return ToResult(
                        status: SyncCityOperationalBudgetPressureStatus.Stale,
                        state: state.OperationalBudgetPressure);
                }

                state.ApplyOperationalBudgetPressure(snapshot: snapshot);

                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    return ToResult(
                        status: SyncCityOperationalBudgetPressureStatus.Applied,
                        state: state.OperationalBudgetPressure);
                }
                catch (Exception exception) when (IsConcurrencyException(exception))
                {
                    SyncCityOperationalBudgetPressureResult? resolved = await ResolveConcurrencyAsync(
                        request: request,
                        simulationHostId: simulationHostId,
                        cancellationToken: cancellationToken);

                    if (resolved is not null)
                        return resolved;

                    if (attempt == MaxSaveAttempts - 1)
                        throw;
                }
            }

            throw new InvalidOperationException("Operational budget pressure synchronization exhausted its save retry budget.");
        }

        private async Task<SyncCityOperationalBudgetPressureResult?> ResolveConcurrencyAsync(
            SyncCityOperationalBudgetPressureCommand request,
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
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
                return ToResult(
                    status: SyncCityOperationalBudgetPressureStatus.Stale,
                    state: persistedState.OperationalBudgetPressure);
            }

            if (MatchesSnapshot(
                    request: request,
                    state: persistedState.OperationalBudgetPressure))
            {
                return ToResult(
                    status: SyncCityOperationalBudgetPressureStatus.Concurrent,
                    state: persistedState.OperationalBudgetPressure);
            }

            return null;
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

        private static SyncCityOperationalBudgetPressureResult ToResult(
            SyncCityOperationalBudgetPressureStatus status,
            CityOperationalBudgetPressureState state)
        {
            return new SyncCityOperationalBudgetPressureResult(
                Status: status,
                PressureIndex: state.PressureIndex,
                EffectiveTickId: state.EffectiveTickId,
                EffectiveAtUtc: state.EffectiveAtUtc);
        }
    }
}
