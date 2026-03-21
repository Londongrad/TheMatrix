using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions
{
    public interface ICityWeatherBootstrapFactory
    {
        CityWeather CreateInitial(
            City city,
            SimTime initialTime);
    }
}
