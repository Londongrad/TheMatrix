using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityResourceSupply
{
    public sealed class SyncCityResourceSupplyCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<SyncCityResourceSupplyCommand, SyncCityResourceSupplyResult>
    {
        private const int MaxSaveAttempts = 3;

        public async Task<SyncCityResourceSupplyResult> Handle(
            SyncCityResourceSupplyCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);
            var resourceSnapshot = new CityResourceSupplySnapshot(
                supplyStressIndex: request.SupplyStressIndex,
                fuelStockLevelIndex: request.FuelStockLevelIndex,
                fuelResupplyReadinessIndex: request.FuelResupplyReadinessIndex,
                fuelShortageRiskIndex: request.FuelShortageRiskIndex,
                sparePartsStockLevelIndex: request.SparePartsStockLevelIndex,
                sparePartsResupplyReadinessIndex: request.SparePartsResupplyReadinessIndex,
                sparePartsShortageRiskIndex: request.SparePartsShortageRiskIndex,
                filtersStockLevelIndex: request.FiltersStockLevelIndex,
                filtersResupplyReadinessIndex: request.FiltersResupplyReadinessIndex,
                filtersShortageRiskIndex: request.FiltersShortageRiskIndex,
                emergencyWaterStockLevelIndex: request.EmergencyWaterStockLevelIndex,
                emergencyWaterResupplyReadinessIndex: request.EmergencyWaterResupplyReadinessIndex,
                emergencyWaterShortageRiskIndex: request.EmergencyWaterShortageRiskIndex,
                effectiveTickId: request.EffectiveTickId,
                effectiveAtUtc: request.EffectiveAtUtc);

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
                    return new SyncCityResourceSupplyResult(
                        Status: SyncCityResourceSupplyStatus.NotInitialized,
                        SupplyStressIndex: 0m,
                        EffectiveTickId: request.EffectiveTickId,
                        EffectiveAtUtc: request.EffectiveAtUtc);

                if (IsIncomingSnapshotStale(
                        effectiveTickId: request.EffectiveTickId,
                        effectiveAtUtc: request.EffectiveAtUtc,
                        currentEffectiveTickId: state.ResourceSupply.EffectiveTickId,
                        currentEffectiveAtUtc: state.ResourceSupply.EffectiveAtUtc))
                    return ToResult(
                        status: SyncCityResourceSupplyStatus.Stale,
                        state: state.ResourceSupply);

                state.ApplyResourceSupply(resourceSnapshot);

                SyncCityResourceSupplyStatus status = SyncCityResourceSupplyStatus.Deferred;

                if (ShouldApplyAtCurrentProgress(
                        effectiveTickId: request.EffectiveTickId,
                        effectiveAtUtc: request.EffectiveAtUtc,
                        lastAppliedTickId: state.LastAppliedTickId,
                        lastEvaluatedAtUtc: state.LastEvaluatedAtUtc))
                {
                    CitySystemPressureProfile pressure = pressureProfileFactory.Create(
                        state: state,
                        asOfUtc: state.LastEvaluatedAtUtc);

                    CityEnvironmentalConditionSnapshot snapshot = policy.Recalculate(
                        state: state,
                        pressure: pressure,
                        asOfUtc: state.LastEvaluatedAtUtc);

                    state.ApplySnapshot(snapshot);
                    status = SyncCityResourceSupplyStatus.Applied;
                }

                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    return ToResult(
                        status: status,
                        state: state.ResourceSupply);
                }
                catch (Exception exception) when (IsConcurrencyException(exception))
                {
                    SyncCityResourceSupplyResult? resolved = await ResolveConcurrencyAsync(
                        request: request,
                        simulationHostId: simulationHostId,
                        cancellationToken: cancellationToken);

                    if (resolved is not null)
                        return resolved;

                    if (attempt == MaxSaveAttempts - 1)
                        throw;
                }
            }

            throw new InvalidOperationException("Resource supply synchronization exhausted its save retry budget.");
        }

        private async Task<SyncCityResourceSupplyResult?> ResolveConcurrencyAsync(
            SyncCityResourceSupplyCommand request,
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken)
        {
            CityEnvironmentalConditionState? persistedState = await repository.GetBySimulationHostIdNoTrackingAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (persistedState is null)
                return new SyncCityResourceSupplyResult(
                    Status: SyncCityResourceSupplyStatus.NotInitialized,
                    SupplyStressIndex: 0m,
                    EffectiveTickId: request.EffectiveTickId,
                    EffectiveAtUtc: request.EffectiveAtUtc);

            if (IsIncomingSnapshotStale(
                    effectiveTickId: request.EffectiveTickId,
                    effectiveAtUtc: request.EffectiveAtUtc,
                    currentEffectiveTickId: persistedState.ResourceSupply.EffectiveTickId,
                    currentEffectiveAtUtc: persistedState.ResourceSupply.EffectiveAtUtc))
                return ToResult(
                    status: SyncCityResourceSupplyStatus.Stale,
                    state: persistedState.ResourceSupply);

            if (MatchesSnapshot(
                    request: request,
                    state: persistedState.ResourceSupply))
                return ToResult(
                    status: SyncCityResourceSupplyStatus.Concurrent,
                    state: persistedState.ResourceSupply);

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

        private static bool ShouldApplyAtCurrentProgress(
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc,
            long lastAppliedTickId,
            DateTimeOffset lastEvaluatedAtUtc)
        {
            if (effectiveTickId < lastAppliedTickId)
                return true;

            if (effectiveTickId > lastAppliedTickId)
                return false;

            return effectiveAtUtc <= lastEvaluatedAtUtc;
        }

        private static bool MatchesSnapshot(
            SyncCityResourceSupplyCommand request,
            CityResourceSupplyState state)
        {
            return state.EffectiveTickId == request.EffectiveTickId &&
                   state.EffectiveAtUtc == request.EffectiveAtUtc &&
                   state.SupplyStressIndex == request.SupplyStressIndex &&
                   state.FuelStockLevelIndex == request.FuelStockLevelIndex &&
                   state.FuelResupplyReadinessIndex == request.FuelResupplyReadinessIndex &&
                   state.FuelShortageRiskIndex == request.FuelShortageRiskIndex &&
                   state.SparePartsStockLevelIndex == request.SparePartsStockLevelIndex &&
                   state.SparePartsResupplyReadinessIndex == request.SparePartsResupplyReadinessIndex &&
                   state.SparePartsShortageRiskIndex == request.SparePartsShortageRiskIndex &&
                   state.FiltersStockLevelIndex == request.FiltersStockLevelIndex &&
                   state.FiltersResupplyReadinessIndex == request.FiltersResupplyReadinessIndex &&
                   state.FiltersShortageRiskIndex == request.FiltersShortageRiskIndex &&
                   state.EmergencyWaterStockLevelIndex == request.EmergencyWaterStockLevelIndex &&
                   state.EmergencyWaterResupplyReadinessIndex == request.EmergencyWaterResupplyReadinessIndex &&
                   state.EmergencyWaterShortageRiskIndex == request.EmergencyWaterShortageRiskIndex;
        }

        private static bool IsConcurrencyException(Exception exception)
        {
            return exception.GetType()
                      .Name ==
                   "DbUpdateConcurrencyException";
        }

        private static SyncCityResourceSupplyResult ToResult(
            SyncCityResourceSupplyStatus status,
            CityResourceSupplyState state)
        {
            return new SyncCityResourceSupplyResult(
                Status: status,
                SupplyStressIndex: state.SupplyStressIndex,
                EffectiveTickId: state.EffectiveTickId,
                EffectiveAtUtc: state.EffectiveAtUtc);
        }
    }
}
