using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure
{
    public sealed class SyncCityOperationalBudgetPressureCommandHandler(
        ICityStockpileRepository repository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<SyncCityOperationalBudgetPressureCommand, SyncCityOperationalBudgetPressureResult>
    {
        public async Task<SyncCityOperationalBudgetPressureResult> Handle(
            SyncCityOperationalBudgetPressureCommand request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);

            CityStockpileState? state = await repository.GetBySimulationHostIdAsync(
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
                    GeneralAvailableAmount: request.GeneralAvailableAmount,
                    OperationsAvailableAmount: request.OperationsAvailableAmount,
                    InfrastructureAvailableAmount: request.InfrastructureAvailableAmount,
                    HealthcareAvailableAmount: request.HealthcareAvailableAmount,
                    GeneralAuthorizationLevel: request.GeneralAuthorizationLevel,
                    OperationsAuthorizationLevel: request.OperationsAuthorizationLevel,
                    InfrastructureAuthorizationLevel: request.InfrastructureAuthorizationLevel,
                    HealthcareAuthorizationLevel: request.HealthcareAuthorizationLevel,
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
