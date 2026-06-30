using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class PendingWeatherHealthImpactStep
    {
        internal static int CalculateHealthDelta(
            PersonEntity person,
            IReadOnlyCollection<CityPopulationPendingWeatherImpact> pendingImpacts,
            CityPopulationWeatherImpactPolicy weatherImpactPolicy)
        {
            if (!person.IsAlive || pendingImpacts.Count == 0)
                return 0;

            int healthDelta = 0;
            foreach (CityPopulationPendingWeatherImpact pendingImpact in pendingImpacts)
                healthDelta += weatherImpactPolicy.CalculateDifferential(
                        person: person,
                        currentDate: pendingImpact.CurrentDate,
                        previousWeather: pendingImpact.PreviousWeather,
                        currentWeather: pendingImpact.CurrentWeather,
                        environment: pendingImpact.Environment)
                   .HealthDelta;

            return Math.Clamp(healthDelta, -100, 100);
        }
    }
}
