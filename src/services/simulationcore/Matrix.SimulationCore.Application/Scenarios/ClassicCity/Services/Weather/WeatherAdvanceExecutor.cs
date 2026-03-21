using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather
{
    /// <summary>
    ///     Advances city weather in lockstep with simulation time.
    /// </summary>
    public sealed class WeatherAdvanceExecutor(
        ICityRepository cityRepository,
        ICityWeatherRepository weatherRepository,
        ICityWeatherBootstrapFactory bootstrapFactory,
        IWeatherStatePlanner planner) : IWeatherAdvanceExecutor
    {
        public async Task<CityWeather?> AdvanceAsync(
            CityId cityId,
            SimTime evaluatedAt,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (city is null)
                return null;

            CityWeather? cityWeather = await weatherRepository.GetByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (cityWeather is null)
            {
                CityWeather initialWeather = bootstrapFactory.CreateInitial(
                    city: city,
                    initialTime: evaluatedAt);

                await weatherRepository.AddAsync(
                    cityWeather: initialWeather,
                    cancellationToken: cancellationToken);

                return initialWeather;
            }

            WeatherState nextNaturalState = planner.PlanNaturalState(
                environment: city.Environment,
                climateProfile: cityWeather.ClimateProfile,
                generationSeed: city.GenerationSeed,
                evaluatedAt: evaluatedAt,
                previousState: cityWeather.ActiveOverride is null
                    ? cityWeather.CurrentState
                    : null);

            cityWeather.AdvanceTo(
                evaluatedAt: evaluatedAt,
                nextState: nextNaturalState);

            return cityWeather;
        }
    }
}
