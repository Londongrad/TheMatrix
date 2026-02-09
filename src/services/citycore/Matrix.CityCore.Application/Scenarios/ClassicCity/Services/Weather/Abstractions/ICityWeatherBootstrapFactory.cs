using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions
{
    public interface ICityWeatherBootstrapFactory
    {
        CityWeather CreateInitial(
            City city,
            SimTime initialTime);
    }
}
