using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Application.Services.Weather.Abstractions
{
    public interface IWeatherStatePlanner
    {
        WeatherState PlanNaturalState(
            CityEnvironment environment,
            WeatherClimateProfile climateProfile,
            SimTime evaluatedAt);
    }
}
