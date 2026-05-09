using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing
{
    public sealed class SetCityEmergencyRationingCommandHandler(
        ICityStockpileRepository repository,
        IUnitOfWork unitOfWork,
        ICityStockpileSnapshotOutboxWriter outboxWriter,
        CityStockpilePolicy policy,
        TimeProvider timeProvider)
        : IRequestHandler<SetCityEmergencyRationingCommand, SetCityEmergencyRationingResult>
    {
        public async Task<SetCityEmergencyRationingResult> Handle(
            SetCityEmergencyRationingCommand request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);

            var state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return new SetCityEmergencyRationingResult(
                    Status: SetCityEmergencyRationingStatus.NotInitialized,
                    CityId: request.CityId,
                    EmergencyRationingEnabled: request.Enabled,
                    SupplyStressIndex: 0m);

            if (state.EmergencyRationingEnabled == request.Enabled)
                return new SetCityEmergencyRationingResult(
                    Status: SetCityEmergencyRationingStatus.Duplicate,
                    CityId: request.CityId,
                    EmergencyRationingEnabled: state.EmergencyRationingEnabled,
                    SupplyStressIndex: state.SupplyStressIndex);

            var refreshedSnapshot = policy.SetEmergencyRationing(
                current: state.ToSnapshot(),
                enabled: request.Enabled);

            state.ApplySnapshot(refreshedSnapshot);
            await outboxWriter.AddClassicCityStockpileSnapshotAsync(
                snapshot: CityStockpileIntegrationEventFactory.CreateSnapshot(
                    state: state,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SetCityEmergencyRationingResult(
                Status: SetCityEmergencyRationingStatus.Applied,
                CityId: request.CityId,
                EmergencyRationingEnabled: state.EmergencyRationingEnabled,
                SupplyStressIndex: state.SupplyStressIndex);
        }
    }
}
