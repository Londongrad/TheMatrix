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

            return segment.Kind switch
            {
                CityWeatherExposureKind.Adverse => CalculateAdverse(
                    person: person,
                    currentDate: currentDate,
                    segment: segment),
                CityWeatherExposureKind.Recovery => CalculateRecovery(
                    person: person,
                    currentDate: currentDate,
                    segment: segment),
                _ => PersonWeatherImpact.None
            };
        }

        private static PersonWeatherImpact CalculateAdverse(
            Person person,
            DateOnly currentDate,
            CityWeatherExposureSegment segment)
        {
            ExposureRule? exposureRule = CreateAdverseRule(
                weather: segment.Weather,
                ageGroup: person.GetAgeGroup(currentDate));

            if (exposureRule is null)
                return PersonWeatherImpact.None;

            decimal startHours = (decimal)(segment.IntervalStartSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;
            decimal endHours = (decimal)(segment.IntervalEndSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;

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

        private static PersonWeatherImpact CalculateRecovery(
            Person person,
            DateOnly currentDate,
            CityWeatherExposureSegment segment)
        {
            if (segment.SourceWeather is null)
                return PersonWeatherImpact.None;

            RecoveryRule? recoveryRule = CreateRecoveryRule(
                currentWeather: segment.Weather,
                sourceWeather: segment.SourceWeather,
                ageGroup: person.GetAgeGroup(currentDate));

            if (recoveryRule is null)
                return PersonWeatherImpact.None;

            decimal startHours = (decimal)(segment.IntervalStartSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;
            decimal endHours = (decimal)(segment.IntervalEndSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;

            if (endHours <= startHours)
                return PersonWeatherImpact.None;

            int completedBlocksAtStart = Math.Min(
                (int)Math.Floor(startHours / recoveryRule.StepHours),
                recoveryRule.MaxBlocks);
            int completedBlocksAtEnd = Math.Min(
                (int)Math.Floor(endHours / recoveryRule.StepHours),
                recoveryRule.MaxBlocks);
            int newlyCompletedBlocks = completedBlocksAtEnd - completedBlocksAtStart;

            if (newlyCompletedBlocks <= 0)
                return PersonWeatherImpact.None;

            int healthDelta = Math.Min(
                newlyCompletedBlocks * recoveryRule.HealthDeltaPerBlock,
                3);
            int happinessDelta = Math.Min(
                newlyCompletedBlocks * recoveryRule.HappinessDeltaPerBlock,
                4);

            return new PersonWeatherImpact(
                HealthDelta: healthDelta,
                HappinessDelta: happinessDelta);
        }

        private static ExposureRule? CreateAdverseRule(
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

        private static RecoveryRule? CreateRecoveryRule(
            WeatherImpactProfile currentWeather,
            WeatherImpactProfile sourceWeather,
            AgeGroup ageGroup)
        {
            if (!IsRecoveryWeather(currentWeather))
                return null;

            if (sourceWeather.Type is not (PopulationWeatherType.Heatwave or PopulationWeatherType.ColdSnap) ||
                sourceWeather.Severity < PopulationWeatherSeverity.Moderate)
                return null;

            bool comfortableRecoveryWeather = IsComfortableRecoveryWeather(currentWeather);
            bool isSensitive = ageGroup is AgeGroup.Child or AgeGroup.Senior;

            decimal stepHours = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 24m,
                PopulationWeatherSeverity.Severe => 24m,
                PopulationWeatherSeverity.Extreme => 12m,
                _ => decimal.MaxValue
            };

            if (stepHours == decimal.MaxValue)
                return null;

            int maxBlocks = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 1,
                PopulationWeatherSeverity.Severe => 2,
                PopulationWeatherSeverity.Extreme => 3,
                _ => 0
            };

            if (maxBlocks == 0)
                return null;

            int healthDeltaPerBlock = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 0,
                PopulationWeatherSeverity.Severe => comfortableRecoveryWeather && !isSensitive
                    ? 1
                    : 0,
                PopulationWeatherSeverity.Extreme => comfortableRecoveryWeather
                    ? 1
                    : 0,
                _ => 0
            };

            int happinessDeltaPerBlock = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 1,
                PopulationWeatherSeverity.Severe => 1,
                PopulationWeatherSeverity.Extreme => 2,
                _ => 0
            };

            if (comfortableRecoveryWeather)
                happinessDeltaPerBlock += 1;

            return new RecoveryRule(
                StepHours: stepHours,
                MaxBlocks: maxBlocks,
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

        private static bool IsRecoveryWeather(WeatherImpactProfile weather)
        {
            return weather.Type is PopulationWeatherType.Clear or PopulationWeatherType.Overcast &&
                   weather.Severity <= PopulationWeatherSeverity.Mild &&
                   weather.PrecipitationKind == PopulationPrecipitationKind.None &&
                   weather.WindSpeedKph <= 25m &&
                   weather.TemperatureC is >= 10m and <= 28m;
        }

        private static bool IsComfortableRecoveryWeather(WeatherImpactProfile weather)
        {
            return weather.Type == PopulationWeatherType.Clear &&
                   weather.Severity <= PopulationWeatherSeverity.Mild &&
                   weather.TemperatureC is >= 18m and <= 24m &&
                   weather.HumidityPercent is >= 35m and <= 65m &&
                   weather.WindSpeedKph <= 20m &&
                   weather.PrecipitationKind == PopulationPrecipitationKind.None;
        }

        private sealed record ExposureRule(
            decimal StepHours,
            int HealthDeltaPerBlock,
            int HappinessDeltaPerBlock);

        private sealed record RecoveryRule(
            decimal StepHours,
            int MaxBlocks,
            int HealthDeltaPerBlock,
            int HappinessDeltaPerBlock);
    }
}
