using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Weather.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Application.Services.Weather
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
        public async Task AdvanceAsync(
            CityId cityId,
            SimTime evaluatedAt,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (city is null)
                return;

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

                initialWeather.ClearDomainEvents();
                return;
            }

            WeatherState nextNaturalState = planner.PlanNaturalState(
                environment: city.Environment,
                climateProfile: cityWeather.ClimateProfile,
                evaluatedAt: evaluatedAt);

            cityWeather.AdvanceTo(
                evaluatedAt: evaluatedAt,
                nextState: nextNaturalState);

            cityWeather.ClearDomainEvents();
        }
    }
}
