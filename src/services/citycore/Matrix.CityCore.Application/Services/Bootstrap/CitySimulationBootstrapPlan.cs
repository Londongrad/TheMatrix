using Matrix.CityCore.Application.Services.Topology;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Application.Services.Bootstrap
{
    public sealed record CitySimulationBootstrapPlan(
        City City,
        SimulationClock Clock,
        CityTopologySeed Topology,
        CityWeather? Weather);
}
