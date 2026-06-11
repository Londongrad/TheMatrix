using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions
{
    public sealed class SeedCityEnvironmentalConditionsCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        ICitySystemsDeletionStateRepository deletionStateRepository,
        IUnitOfWork unitOfWork,
        ICityPopulationLivingConditionsOutboxWriter populationLivingConditionsOutboxWriter,
        CityEnvironmentalConditionPolicy policy,
        TimeProvider timeProvider)
        : IRequestHandler<SeedCityEnvironmentalConditionsCommand, SeedCityEnvironmentalConditionsResult>
    {
        public async Task<SeedCityEnvironmentalConditionsResult> Handle(
            SeedCityEnvironmentalConditionsCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            DateTimeOffset? deletedAtUtc = await deletionStateRepository.GetDeletedAtUtcAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            if (deletedAtUtc.HasValue)
                return new SeedCityEnvironmentalConditionsResult(
                    Status: SeedCityEnvironmentalConditionsStatus.CityDeleted,
                    LastEvaluatedAtUtc: deletedAtUtc.Value);

            CityEnvironmentalConditionState? existing = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (existing is not null)
                return new SeedCityEnvironmentalConditionsResult(
                    Status: SeedCityEnvironmentalConditionsStatus.Duplicate,
                    LastEvaluatedAtUtc: existing.LastEvaluatedAtUtc);

            CityEnvironmentalConditionSnapshot seed = policy.CreateSeed(
                cityId: request.CityId,
                developmentLevel: request.DevelopmentLevel,
                asOfUtc: request.CreatedAtUtc);
            var state = CityEnvironmentalConditionState.Create(
                simulationHostId: simulationHostId,
                seed: seed);

            await repository.AddAsync(
                state: state,
                cancellationToken: cancellationToken);
            await populationLivingConditionsOutboxWriter.AddClassicCityLivingConditionsSnapshotAsync(
                snapshot: CityPopulationLivingConditionsIntegrationEventFactory.CreateSnapshot(
                    state: state,
                    occurredAtUtc: timeProvider.GetUtcNow()),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SeedCityEnvironmentalConditionsResult(
                Status: SeedCityEnvironmentalConditionsStatus.Applied,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc);
        }
    }
}
