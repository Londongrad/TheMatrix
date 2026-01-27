using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Common;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;
using Matrix.CityCore.Domain.Weather.Enums;

namespace Matrix.CityCore.Domain.Events.Weather
{
    public sealed record WeatherOverrideCancelledDomainEvent(
        CityId CityId,
        WeatherState ForcedState,
        WeatherOverrideSource Source,
        SimTime CancelledAt) : DomainEventBase;
}