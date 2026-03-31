using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions
{
    public sealed class SeedCityEnvironmentalConditionsCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        ICityPopulationLivingConditionsOutboxWriter populationLivingConditionsOutboxWriter,
        CityEnvironmentalConditionPolicy policy)
        : IRequestHandler<SeedCityEnvironmentalConditionsCommand, SeedCityEnvironmentalConditionsResult>
    {
        public async Task<SeedCityEnvironmentalConditionsResult> Handle(
            SeedCityEnvironmentalConditionsCommand request,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityScenario.IsMatch(request.SimulationKind))
            {
                return new SeedCityEnvironmentalConditionsResult(
                    Status: SeedCityEnvironmentalConditionsStatus.IgnoredSimulationKind,
                    LastEvaluatedAtUtc: request.CreatedAtUtc);
            }

            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? existing = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (existing is not null)
            {
                return new SeedCityEnvironmentalConditionsResult(
                    Status: SeedCityEnvironmentalConditionsStatus.Duplicate,
                    LastEvaluatedAtUtc: existing.LastEvaluatedAtUtc);
            }

            var seed = policy.CreateSeed(
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
                    occurredAtUtc: DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new SeedCityEnvironmentalConditionsResult(
                Status: SeedCityEnvironmentalConditionsStatus.Applied,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc);
        }
    }
}
