using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.AdvanceCityEnvironmentalConditions
{
    public sealed class AdvanceCityEnvironmentalConditionsCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<AdvanceCityEnvironmentalConditionsCommand, AdvanceCityEnvironmentalConditionsResult>
    {
        public async Task<AdvanceCityEnvironmentalConditionsResult> Handle(
            AdvanceCityEnvironmentalConditionsCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
            {
                return new AdvanceCityEnvironmentalConditionsResult(
                    Status: AdvanceCityEnvironmentalConditionsStatus.NotInitialized,
                    ProcessedSimMinutes: 0m,
                    FloodingIndex: 0m,
                    SnowAccumulationIndex: 0m,
                    RoadAccessibilityIndex: 0m);
            }

            if (request.ToSimTimeUtc < state.LastEvaluatedAtUtc)
                return CreateResult(
                    status: AdvanceCityEnvironmentalConditionsStatus.OutOfOrder,
                    processedSimMinutes: 0m,
                    state: state);

            DateTimeOffset effectiveFrom = request.FromSimTimeUtc > state.LastEvaluatedAtUtc
                ? request.FromSimTimeUtc
                : state.LastEvaluatedAtUtc;

            if (request.ToSimTimeUtc <= effectiveFrom)
                return CreateResult(
                    status: AdvanceCityEnvironmentalConditionsStatus.Duplicate,
                    processedSimMinutes: 0m,
                    state: state);

            decimal processedSimMinutes = decimal.Round(
                d: (decimal)(request.ToSimTimeUtc - effectiveFrom).TotalMinutes,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);

            var pressure = pressureProfileFactory.Create(
                state: state,
                asOfUtc: request.ToSimTimeUtc);

            var snapshot = policy.Advance(
                state: state,
                pressure: pressure,
                fromUtc: effectiveFrom,
                toUtc: request.ToSimTimeUtc);

            state.ApplySnapshot(snapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CreateResult(
                status: AdvanceCityEnvironmentalConditionsStatus.Applied,
                processedSimMinutes: processedSimMinutes,
                state: state);
        }

        private static AdvanceCityEnvironmentalConditionsResult CreateResult(
            AdvanceCityEnvironmentalConditionsStatus status,
            decimal processedSimMinutes,
            CityEnvironmentalConditionState state)
        {
            return new AdvanceCityEnvironmentalConditionsResult(
                Status: status,
                ProcessedSimMinutes: processedSimMinutes,
                FloodingIndex: state.FloodingIndex.Value,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value);
        }
    }
}
