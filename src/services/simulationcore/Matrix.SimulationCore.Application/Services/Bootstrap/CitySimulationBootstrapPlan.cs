using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Bootstrap
{
    public sealed record CitySimulationBootstrapPlan(
        SimulationInstance Instance,
        City City,
        SimulationClock Clock,
        CityTopologySeed Topology,
        CityWeather? Weather,
        bool SupportsAutomaticPopulationBootstrap);
}
