using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentWeatherExposureStep
    {
        internal static ResidentProgressionStepResult Apply(
            PersonEntity person,
            DateOnly currentDate,
            CityPopulationEnvironment? environment,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            CityPopulationWeatherExposurePolicy weatherExposurePolicy)
        {
            if (exposureSegments.Count == 0 || !person.IsAlive)
                return ResidentProgressionStepResult.None;
            int totalHealthDelta = 0;
            int totalHappinessDelta = 0;
            foreach (CityWeatherExposureSegment segment in exposureSegments)
            {
                PersonWeatherImpact impact = weatherExposurePolicy.Calculate(
                    person: person,
                    currentDate: currentDate,
                    segment: segment,
                    environment: environment);
                totalHealthDelta += impact.HealthDelta;
                totalHappinessDelta += impact.HappinessDelta;
            }

            if (totalHealthDelta == 0 && totalHappinessDelta == 0)
                return ResidentProgressionStepResult.None;

            bool populationChanged = false;
            if (totalHappinessDelta != 0 && person.IsAlive)
            {
                int previousHappiness = person.Happiness.Value;
                person.ChangeHappiness(totalHappinessDelta);
                populationChanged = previousHappiness != person.Happiness.Value;
            }

            return new ResidentProgressionStepResult(
                PopulationChanged: populationChanged,
                ExternalHealthDelta: Math.Clamp(totalHealthDelta, -100, 100));
        }
    }
}
