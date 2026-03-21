using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions
{
    public interface IWeatherStatePlanner
    {
        WeatherState PlanNaturalState(
            CityEnvironment environment,
            WeatherClimateProfile climateProfile,
            CityGenerationSeed generationSeed,
            SimTime evaluatedAt,
            WeatherState? previousState = null);
    }
}
