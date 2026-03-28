using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand
{
    public sealed class SyncCitySystemsDemandCommandHandler(
        ICityStockpileRepository repository,
        IUnitOfWork unitOfWork,
        CityStockpilePolicy policy)
        : IRequestHandler<SyncCitySystemsDemandCommand, SyncCitySystemsDemandResult>
    {
        public async Task<SyncCitySystemsDemandResult> Handle(
            SyncCitySystemsDemandCommand request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);

            CityStockpileState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
            {
                return new SyncCitySystemsDemandResult(
                    Status: SyncCitySystemsDemandStatus.NotInitialized,
                    OverallDemandPressureIndex: 0m,
                    EffectiveAtUtc: request.EffectiveAtUtc);
            }

            if (request.EffectiveAtUtc < state.SystemsDemand.EffectiveAtUtc)
            {
                return new SyncCitySystemsDemandResult(
                    Status: SyncCitySystemsDemandStatus.Stale,
                    OverallDemandPressureIndex: state.SystemsDemand.OverallDemandPressureIndex,
                    EffectiveAtUtc: state.SystemsDemand.EffectiveAtUtc);
            }

            CitySystemsResourceDemandSnapshot demandSnapshot = new(
                FuelDemandPressureIndex: request.FuelDemandPressureIndex,
                SparePartsDemandPressureIndex: request.SparePartsDemandPressureIndex,
                FiltersDemandPressureIndex: request.FiltersDemandPressureIndex,
                EmergencyWaterDemandPressureIndex: request.EmergencyWaterDemandPressureIndex,
                OverallDemandPressureIndex: request.OverallDemandPressureIndex,
                EffectiveAtUtc: request.EffectiveAtUtc);

            state.ApplySystemsDemand(demandSnapshot);

            SyncCitySystemsDemandStatus status = SyncCitySystemsDemandStatus.Deferred;

            if (request.EffectiveAtUtc <= state.LastEvaluatedAtUtc)
            {
                CityStockpileSnapshot refreshedSnapshot = policy.ApplySystemsDemand(state.ToSnapshot());
                state.ApplySnapshot(refreshedSnapshot);
                status = SyncCitySystemsDemandStatus.Applied;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SyncCitySystemsDemandResult(
                Status: status,
                OverallDemandPressureIndex: state.SystemsDemand.OverallDemandPressureIndex,
                EffectiveAtUtc: state.SystemsDemand.EffectiveAtUtc);
        }
    }
}
