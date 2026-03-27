using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
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
                    SupplyStressIndex: 0m,
                    FuelStockLevelIndex: 0m,
                    FoodStockLevelIndex: 0m,
                    EmergencyWaterStockLevelIndex: 0m);

            var refreshedSnapshot = policy.DispatchResupply(
                current: state.ToSnapshot(),
                focus: request.Focus,
                intensity: request.Intensity);

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
                    intensity: request.Intensity,
                    occurredAtUtc: occurredAtUtc),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DispatchCityResupplyResult(
                Status: DispatchCityResupplyStatus.Applied,
                CityId: request.CityId,
                SupplyStressIndex: state.SupplyStressIndex,
                FuelStockLevelIndex: state.Fuel.StockLevelIndex,
                FoodStockLevelIndex: state.Food.StockLevelIndex,
                EmergencyWaterStockLevelIndex: state.EmergencyWater.StockLevelIndex);
        }
    }
}
