using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles
{
    public sealed class SeedCityStockpilesCommandHandler(
        ICityStockpileRepository repository,
        ICityResourceDeletionStateRepository deletionStateRepository,
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
            SimulationHostId simulationHostId = new(request.CityId);

            DateTimeOffset? deletedAtUtc = await deletionStateRepository.GetDeletedAtUtcAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            if (deletedAtUtc.HasValue)
                return new SeedCityStockpilesResult(
                    Status: SeedCityStockpilesStatus.CityDeleted,
                    CityId: request.CityId,
                    SupplyStressIndex: 0m,
                    EmergencyRationingEnabled: false);

            CityStockpileState? existing = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (existing is not null)
                return new SeedCityStockpilesResult(
                    Status: SeedCityStockpilesStatus.Duplicate,
                    CityId: request.CityId,
                    SupplyStressIndex: existing.SupplyStressIndex,
                    EmergencyRationingEnabled: existing.EmergencyRationingEnabled);

            CityStockpileSnapshot seed = policy.CreateSeed(
                developmentLevel: request.DevelopmentLevel,
                createdAtUtc: request.CreatedAtUtc);

            var state = CityStockpileState.Create(
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
