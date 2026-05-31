using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Bootstrap
{
    public sealed record ClassicCityBootstrapPlan(
        SimulationInstance Instance,
        City City,
        SimulationClock Clock,
        CityTopologySeed Topology,
        CityWeather? Weather,
        bool SupportsAutomaticPopulationBootstrap);
}
