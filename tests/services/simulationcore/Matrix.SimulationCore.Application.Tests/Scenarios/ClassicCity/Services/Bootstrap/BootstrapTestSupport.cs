using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Bootstrap
{
    internal static class BootstrapTestSupport
    {
        internal sealed class FakeCityTopologyBootstrapFactory : ICityTopologyBootstrapFactory
        {
            public City? RequestedCity { get; private set; }
            public required CityTopologySeed Result { get; init; }

            public CityTopologySeed CreateInitial(City city)
            {
                RequestedCity = city;
                return Result;
            }
        }

        internal sealed class FakeCityWeatherBootstrapFactory : ICityWeatherBootstrapFactory
        {
            public City? RequestedCity { get; private set; }
            public SimTime? RequestedInitialTime { get; private set; }
            public required Func<City, SimTime, CityWeather> Factory { get; init; }

            public CityWeather CreateInitial(
                City city,
                SimTime initialTime)
            {
                RequestedCity = city;
                RequestedInitialTime = initialTime;
                return Factory(
                    arg1: city,
                    arg2: initialTime);
            }
        }
    }
}
