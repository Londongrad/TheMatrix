using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles
{
    public sealed class AdvanceCityStockpilesCommandHandler(
        ICityStockpileRepository repository,
        IUnitOfWork unitOfWork,
        CityStockpilePolicy policy)
        : IRequestHandler<AdvanceCityStockpilesCommand, AdvanceCityStockpilesResult>
    {
        public async Task<AdvanceCityStockpilesResult> Handle(
            AdvanceCityStockpilesCommand request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);

            CityStockpileState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return CreateResult(
                    status: AdvanceCityStockpilesStatus.NotInitialized,
                    cityId: request.CityId);

            if (request.ToSimTimeUtc < state.LastEvaluatedAtUtc)
                return CreateResult(
                    status: AdvanceCityStockpilesStatus.OutOfOrder,
                    cityId: request.CityId);

            DateTimeOffset effectiveFrom = request.FromSimTimeUtc > state.LastEvaluatedAtUtc
                ? request.FromSimTimeUtc
                : state.LastEvaluatedAtUtc;

            if (request.ToSimTimeUtc <= effectiveFrom)
                return CreateResult(
                    status: AdvanceCityStockpilesStatus.Duplicate,
                    cityId: request.CityId,
                    state: state);

            CityStockpileSnapshot refreshedSnapshot = policy.Advance(
                current: state.ToSnapshot(),
                elapsed: request.ToSimTimeUtc - effectiveFrom);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            long processedSimMinutes = (long)Math.Round(
                value: (request.ToSimTimeUtc - effectiveFrom).TotalMinutes,
                MidpointRounding.AwayFromZero);

            return CreateResult(
                status: AdvanceCityStockpilesStatus.Applied,
                cityId: request.CityId,
                state: state,
                processedSimMinutes: processedSimMinutes);
        }

        private static AdvanceCityStockpilesResult CreateResult(
            AdvanceCityStockpilesStatus status,
            Guid cityId,
            CityStockpileState? state = null,
            long processedSimMinutes = 0)
        {
            return new AdvanceCityStockpilesResult(
                Status: status,
                CityId: cityId,
                ProcessedSimMinutes: processedSimMinutes,
                SupplyStressIndex: state?.SupplyStressIndex ?? 0m,
                FuelStockLevelIndex: state?.Fuel.StockLevelIndex ?? 0m,
                FoodStockLevelIndex: state?.Food.StockLevelIndex ?? 0m,
                EmergencyWaterStockLevelIndex: state?.EmergencyWater.StockLevelIndex ?? 0m);
        }
    }
}
