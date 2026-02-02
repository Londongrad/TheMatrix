using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Domain.Services
{
    public sealed class CityPopulationWeatherExposurePolicy
    {
        public PersonWeatherImpact Calculate(
            Person person,
            DateOnly currentDate,
            CityWeatherExposureSegment segment)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentNullException.ThrowIfNull(segment);

            if (!person.IsAlive)
                return PersonWeatherImpact.None;

            ExposureRule? exposureRule = CreateRule(
                weather: segment.Weather,
                ageGroup: person.GetAgeGroup(currentDate));

            if (exposureRule is null)
                return PersonWeatherImpact.None;

            decimal startHours = (decimal)(segment.IntervalStartSimTimeUtc - segment.WeatherEffectiveAtSimTimeUtc).TotalHours;
            decimal endHours = (decimal)(segment.IntervalEndSimTimeUtc - segment.WeatherEffectiveAtSimTimeUtc).TotalHours;

            if (endHours <= startHours)
                return PersonWeatherImpact.None;

            int completedBlocksAtStart = (int)Math.Floor(startHours / exposureRule.StepHours);
            int completedBlocksAtEnd = (int)Math.Floor(endHours / exposureRule.StepHours);
            int newlyCompletedBlocks = completedBlocksAtEnd - completedBlocksAtStart;

            if (newlyCompletedBlocks <= 0)
                return PersonWeatherImpact.None;

            int healthDelta = Math.Max(
                val1: newlyCompletedBlocks * exposureRule.HealthDeltaPerBlock,
                val2: -6);
            int happinessDelta = Math.Max(
                val1: newlyCompletedBlocks * exposureRule.HappinessDeltaPerBlock,
                val2: -6);

            return new PersonWeatherImpact(
                HealthDelta: healthDelta,
                HappinessDelta: happinessDelta);
        }

        private static ExposureRule? CreateRule(
            WeatherImpactProfile weather,
            AgeGroup ageGroup)
        {
            if (weather.Type is not (PopulationWeatherType.Heatwave or PopulationWeatherType.ColdSnap))
                return null;

            if (weather.Severity is PopulationWeatherSeverity.Unknown
                or PopulationWeatherSeverity.Calm
                or PopulationWeatherSeverity.Mild)
                return null;

            bool isSensitive = ageGroup is AgeGroup.Child or AgeGroup.Senior;

            decimal stepHours = weather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 24m,
                PopulationWeatherSeverity.Severe => 12m,
                PopulationWeatherSeverity.Extreme => 6m,
                _ => decimal.MaxValue
            };

            if (stepHours == decimal.MaxValue)
                return null;

            int healthDeltaPerBlock = weather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => isSensitive
                    ? -1
                    : 0,
                PopulationWeatherSeverity.Severe => -1,
                PopulationWeatherSeverity.Extreme => isSensitive
                    ? -2
                    : -1,
                _ => 0
            };

            int happinessDeltaPerBlock = weather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => -1,
                PopulationWeatherSeverity.Severe => -1,
                PopulationWeatherSeverity.Extreme => -2,
                _ => 0
            };

            if (IsExtremeTemperature(weather))
            {
                if (isSensitive)
                    healthDeltaPerBlock -= 1;

                happinessDeltaPerBlock -= 1;
            }

            return new ExposureRule(
                StepHours: stepHours,
                HealthDeltaPerBlock: healthDeltaPerBlock,
                HappinessDeltaPerBlock: happinessDeltaPerBlock);
        }

        private static bool IsExtremeTemperature(WeatherImpactProfile weather)
        {
            return weather.Type switch
            {
                PopulationWeatherType.Heatwave => weather.TemperatureC >= 38m,
                PopulationWeatherType.ColdSnap => weather.TemperatureC <= -20m,
                _ => false
            };
        }

        private sealed record ExposureRule(
            decimal StepHours,
            int HealthDeltaPerBlock,
            int HappinessDeltaPerBlock);
    }
}
