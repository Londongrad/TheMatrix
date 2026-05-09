using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles
{
    public sealed class SeedCityStockpilesCommandHandler(
        ICityStockpileRepository repository,
        IUnitOfWork unitOfWork,
        ICityStockpileSnapshotOutboxWriter outboxWriter,
        CityStockpilePolicy policy,
        TimeProvider timeProvider)
        : IRequestHandler<SeedCityStockpilesCommand, SeedCityStockpilesResult>
    {
        public async Task<SeedCityStockpilesResult> Handle(
            SeedCityStockpilesCommand request,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityScenario.IsMatch(request.SimulationKind))
                return new SeedCityStockpilesResult(
                    Status: SeedCityStockpilesStatus.IgnoredSimulationKind,
                    CityId: request.CityId,
                    SupplyStressIndex: 0m,
                    EmergencyRationingEnabled: false);

            SimulationHostId simulationHostId = new(request.CityId);

            CityStockpileState? existing = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (existing is not null)
                return new SeedCityStockpilesResult(
                    Status: SeedCityStockpilesStatus.Duplicate,
                    CityId: request.CityId,
                    SupplyStressIndex: existing.SupplyStressIndex,
                    EmergencyRationingEnabled: existing.EmergencyRationingEnabled);

            var seed = policy.CreateSeed(
                developmentLevel: request.DevelopmentLevel,
                createdAtUtc: request.CreatedAtUtc);

            CityStockpileState state = CityStockpileState.Create(
                simulationHostId: simulationHostId,
                seed: seed);

            await repository.AddAsync(
                state: state,
                cancellationToken: cancellationToken);
            await outboxWriter.AddClassicCityStockpileSnapshotAsync(
                snapshot: CityStockpileIntegrationEventFactory.CreateSnapshot(
                    state: state,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SeedCityStockpilesResult(
                Status: SeedCityStockpilesStatus.Applied,
                CityId: request.CityId,
                SupplyStressIndex: state.SupplyStressIndex,
                EmergencyRationingEnabled: state.EmergencyRationingEnabled);
        }
    }
}
