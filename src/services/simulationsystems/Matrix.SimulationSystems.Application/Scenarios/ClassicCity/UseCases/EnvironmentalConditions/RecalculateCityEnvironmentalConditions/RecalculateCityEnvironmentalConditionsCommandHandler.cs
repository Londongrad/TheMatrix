using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions
{
    public sealed class RecalculateCityEnvironmentalConditionsCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<RecalculateCityEnvironmentalConditionsCommand, RecalculateCityEnvironmentalConditionsResult>
    {
        public async Task<RecalculateCityEnvironmentalConditionsResult> Handle(
            RecalculateCityEnvironmentalConditionsCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return new RecalculateCityEnvironmentalConditionsResult(
                    Status: RecalculateCityEnvironmentalConditionsStatus.NotInitialized,
                    FloodingIndex: 0m,
                    SnowAccumulationIndex: 0m,
                    RoadAccessibilityIndex: 0m);

            if (request.AtSimTimeUtc < state.LastEvaluatedAtUtc)
                return CreateResult(
                    status: RecalculateCityEnvironmentalConditionsStatus.Stale,
                    state: state);

            CityWeatherPressureProfile weatherPressure = pressureProfileFactory.CreateWeatherPressure(request.Weather);
            state.ApplyWeatherPressure(weatherPressure);

            if (request.AtSimTimeUtc == state.LastEvaluatedAtUtc)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return CreateResult(
                    status: RecalculateCityEnvironmentalConditionsStatus.Duplicate,
                    state: state);
            }

            CitySystemPressureProfile pressure = pressureProfileFactory.Create(
                state: state,
                asOfUtc: request.AtSimTimeUtc);

            CityEnvironmentalConditionSnapshot snapshot = policy.Recalculate(
                state: state,
                pressure: pressure,
                asOfUtc: request.AtSimTimeUtc);

            state.ApplySnapshot(snapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return CreateResult(
                status: RecalculateCityEnvironmentalConditionsStatus.Applied,
                state: state);
        }

        private static RecalculateCityEnvironmentalConditionsResult CreateResult(
            RecalculateCityEnvironmentalConditionsStatus status,
            CityEnvironmentalConditionState state)
        {
            return new RecalculateCityEnvironmentalConditionsResult(
                Status: status,
                FloodingIndex: state.FloodingIndex.Value,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value);
        }
    }
}
