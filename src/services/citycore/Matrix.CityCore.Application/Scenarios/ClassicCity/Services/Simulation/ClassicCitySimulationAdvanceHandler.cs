using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Events.Simulation;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Simulation
{
    public sealed class ClassicCitySimulationAdvanceHandler(
        IWeatherAdvanceExecutor weatherAdvanceExecutor,
        ICityCoreOutboxWriter outboxWriter) : ISimulationScenarioAdvanceHandler
    {
        public SimulationHostKind HostKind => SimulationHostKind.City;

        public async Task HandleAdvancedAsync(
            SimulationHost host,
            SimulationTimeAdvancedDomainEvent advancedEvent,
            CancellationToken cancellationToken)
        {
            CityId cityId = new(host.HostId.Value);

            CityWeather? cityWeather = await weatherAdvanceExecutor.AdvanceAsync(
                cityId: cityId,
                evaluatedAt: advancedEvent.To,
                cancellationToken: cancellationToken);

            await outboxWriter.AddCityTimeAdvancedAsync(
                cityId: cityId,
                from: advancedEvent.From,
                to: advancedEvent.To,
                tickId: advancedEvent.TickId,
                speed: advancedEvent.Speed,
                cancellationToken: cancellationToken);

            if (cityWeather is null || cityWeather.DomainEvents.Count == 0)
                return;

            await outboxWriter.AddWeatherEventsAsync(
                domainEvents: cityWeather.DomainEvents,
                cancellationToken: cancellationToken);
            cityWeather.ClearDomainEvents();
        }
    }
}
