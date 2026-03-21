using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather
{
    public sealed record WeatherOverrideStartedDomainEvent(
        CityId CityId,
        WeatherState ForcedState,
        WeatherOverrideSource Source,
        SimTime StartsAt,
        SimTime EndsAt,
        string? Reason) : DomainEventBase;
}
