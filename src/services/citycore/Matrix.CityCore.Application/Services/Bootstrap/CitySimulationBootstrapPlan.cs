using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Bootstrap
{
    public sealed record CitySimulationBootstrapPlan(
        City City,
        SimulationClock Clock,
        CityTopologySeed Topology,
        CityWeather? Weather,
        bool SupportsAutomaticPopulationBootstrap);
}
