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
                    EffectiveAtUtc: request.EffectiveAtUtc);
            }

            if (request.EffectiveAtUtc < state.OperationalBudgetPressure.EffectiveAtUtc)
            {
                return new SyncCityOperationalBudgetPressureResult(
                    Status: SyncCityOperationalBudgetPressureStatus.Stale,
                    PressureIndex: state.OperationalBudgetPressure.PressureIndex,
                    EffectiveAtUtc: state.OperationalBudgetPressure.EffectiveAtUtc);
            }

            state.ApplyOperationalBudgetPressure(
                snapshot: new CityOperationalBudgetPressureSnapshot(
                    Balance: request.Balance,
                    MunicipalOperationsExpenses: request.MunicipalOperationsExpenses,
                    PressureIndex: request.PressureIndex,
                    EffectiveAtUtc: request.EffectiveAtUtc));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SyncCityOperationalBudgetPressureResult(
                Status: SyncCityOperationalBudgetPressureStatus.Applied,
                PressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EffectiveAtUtc: state.OperationalBudgetPressure.EffectiveAtUtc);
        }
    }
}
