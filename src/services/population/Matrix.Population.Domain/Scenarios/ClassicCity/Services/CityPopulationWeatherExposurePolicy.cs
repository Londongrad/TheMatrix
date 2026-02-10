using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Domain.Services
{
    public sealed class CityPopulationWeatherExposurePolicy(
        CityPopulationClimateAdaptationPolicy climateAdaptationPolicy)
    {
        public PersonWeatherImpact Calculate(
            Person person,
            DateOnly currentDate,
            CityWeatherExposureSegment segment,
            CityPopulationEnvironment? environment)
        {
            person = GuardHelper.AgainstNull(
                value: person,
                propertyName: nameof(person));
            segment = GuardHelper.AgainstNull(
                value: segment,
                propertyName: nameof(segment));

            if (!person.IsAlive)
                return PersonWeatherImpact.None;

            return segment.Kind switch
            {
                CityWeatherExposureKind.Adverse => CalculateAdverse(
                    person: person,
                    currentDate: currentDate,
                    segment: segment,
                    environment: environment,
                    climateAdaptationPolicy: climateAdaptationPolicy),
                CityWeatherExposureKind.Recovery => CalculateRecovery(
                    person: person,
                    currentDate: currentDate,
                    segment: segment,
                    environment: environment,
                    climateAdaptationPolicy: climateAdaptationPolicy),
                _ => PersonWeatherImpact.None
            };
        }

        private static PersonWeatherImpact CalculateAdverse(
            Person person,
            DateOnly currentDate,
            CityWeatherExposureSegment segment,
            CityPopulationEnvironment? environment,
            CityPopulationClimateAdaptationPolicy climateAdaptationPolicy)
        {
            ExposureRule? exposureRule = CreateAdverseRule(
                weather: segment.Weather,
                ageGroup: person.GetAgeGroup(currentDate));

            if (exposureRule is null)
                return PersonWeatherImpact.None;

            int toleranceScore = climateAdaptationPolicy.GetToleranceScore(
                environment: environment,
                currentDate: currentDate,
                weatherType: segment.Weather.Type);

            ExposureRule adaptedRule = new(
                StepHours: climateAdaptationPolicy.AdjustExposureStepHours(
                    stepHours: exposureRule.StepHours,
                    toleranceScore: toleranceScore),
                HealthDeltaPerBlock: climateAdaptationPolicy.AdjustNegativeDelta(
                    delta: exposureRule.HealthDeltaPerBlock,
                    toleranceScore: toleranceScore),
                HappinessDeltaPerBlock: climateAdaptationPolicy.AdjustNegativeDelta(
                    delta: exposureRule.HappinessDeltaPerBlock,
                    toleranceScore: toleranceScore));

            decimal startHours =
                (decimal)(segment.IntervalStartSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;
            decimal endHours = (decimal)(segment.IntervalEndSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;

            if (endHours <= startHours)
                return PersonWeatherImpact.None;

            int completedBlocksAtStart = (int)Math.Floor(startHours / adaptedRule.StepHours);
            int completedBlocksAtEnd = (int)Math.Floor(endHours / adaptedRule.StepHours);
            int newlyCompletedBlocks = completedBlocksAtEnd - completedBlocksAtStart;

            if (newlyCompletedBlocks <= 0)
                return PersonWeatherImpact.None;

            int healthDelta = Math.Max(
                val1: newlyCompletedBlocks * adaptedRule.HealthDeltaPerBlock,
                val2: -6);
            int happinessDelta = Math.Max(
                val1: newlyCompletedBlocks * adaptedRule.HappinessDeltaPerBlock,
                val2: -6);

            return new PersonWeatherImpact(
                HealthDelta: healthDelta,
                HappinessDelta: happinessDelta);
        }

        private static PersonWeatherImpact CalculateRecovery(
            Person person,
            DateOnly currentDate,
            CityWeatherExposureSegment segment,
            CityPopulationEnvironment? environment,
            CityPopulationClimateAdaptationPolicy climateAdaptationPolicy)
        {
            if (segment.SourceWeather is null)
                return PersonWeatherImpact.None;

            RecoveryRule? recoveryRule = CreateRecoveryRule(
                currentWeather: segment.Weather,
                sourceWeather: segment.SourceWeather,
                ageGroup: person.GetAgeGroup(currentDate));

            if (recoveryRule is null)
                return PersonWeatherImpact.None;

            int toleranceScore = climateAdaptationPolicy.GetToleranceScore(
                environment: environment,
                currentDate: currentDate,
                weatherType: segment.SourceWeather.Type);

            decimal startHours =
                (decimal)(segment.IntervalStartSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;
            decimal endHours = (decimal)(segment.IntervalEndSimTimeUtc - segment.EffectStartedAtSimTimeUtc).TotalHours;

            if (endHours <= startHours)
                return PersonWeatherImpact.None;

            int completedBlocksAtStart = Math.Min(
                val1: (int)Math.Floor(startHours / recoveryRule.StepHours),
                val2: recoveryRule.MaxBlocks);
            int completedBlocksAtEnd = Math.Min(
                val1: (int)Math.Floor(endHours / recoveryRule.StepHours),
                val2: recoveryRule.MaxBlocks);
            int newlyCompletedBlocks = completedBlocksAtEnd - completedBlocksAtStart;

            if (newlyCompletedBlocks <= 0)
                return PersonWeatherImpact.None;

            int healthDelta = Math.Min(
                val1: newlyCompletedBlocks * recoveryRule.HealthDeltaPerBlock,
                val2: 3);
            int happinessDelta = climateAdaptationPolicy.AdjustPositiveReliefDelta(
                delta: Math.Min(
                    val1: newlyCompletedBlocks * recoveryRule.HappinessDeltaPerBlock,
                    val2: 4),
                toleranceScore: toleranceScore);

            return new PersonWeatherImpact(
                HealthDelta: healthDelta,
                HappinessDelta: happinessDelta);
        }

        private static ExposureRule? CreateAdverseRule(
            WeatherImpactProfile weather,
            AgeGroup ageGroup)
        {
            if (!CityWeatherExposureRules.IsAdverseExposureWeather(weather))
                return null;

            bool isSensitive = ageGroup is AgeGroup.Child or AgeGroup.Senior;
            return weather.Type switch
            {
                PopulationWeatherType.Heatwave or PopulationWeatherType.ColdSnap => CreateTemperatureStressRule(
                    weather: weather,
                    isSensitive: isSensitive),
                PopulationWeatherType.Storm => CreateStormStressRule(
                    weather: weather,
                    isSensitive: isSensitive),
                PopulationWeatherType.Snow => CreateSnowStressRule(
                    weather: weather,
                    isSensitive: isSensitive),
                _ => null
            };
        }

        private static RecoveryRule? CreateRecoveryRule(
            WeatherImpactProfile currentWeather,
            WeatherImpactProfile sourceWeather,
            AgeGroup ageGroup)
        {
            if (!CityWeatherExposureRules.IsRecoveryWeather(currentWeather))
                return null;

            if (!CityWeatherExposureRules.IsAdverseExposureWeather(sourceWeather))
                return null;

            bool comfortableRecoveryWeather = CityWeatherExposureRules.IsComfortableRecoveryWeather(currentWeather);
            bool isSensitive = ageGroup is AgeGroup.Child or AgeGroup.Senior;

            return sourceWeather.Type switch
            {
                PopulationWeatherType.Heatwave or PopulationWeatherType.ColdSnap => CreateTemperatureRecoveryRule(
                    sourceWeather: sourceWeather,
                    comfortableRecoveryWeather: comfortableRecoveryWeather,
                    isSensitive: isSensitive),
                PopulationWeatherType.Storm => CreateStormRecoveryRule(
                    sourceWeather: sourceWeather,
                    comfortableRecoveryWeather: comfortableRecoveryWeather,
                    isSensitive: isSensitive),
                PopulationWeatherType.Snow => CreateSnowRecoveryRule(
                    sourceWeather: sourceWeather,
                    comfortableRecoveryWeather: comfortableRecoveryWeather,
                    isSensitive: isSensitive),
                _ => null
            };
        }

        private static ExposureRule? CreateTemperatureStressRule(
            WeatherImpactProfile weather,
            bool isSensitive)
        {
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

        private static ExposureRule? CreateStormStressRule(
            WeatherImpactProfile weather,
            bool isSensitive)
        {
            decimal stepHours = weather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 12m,
                PopulationWeatherSeverity.Severe => 8m,
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
                PopulationWeatherSeverity.Severe => -2,
                PopulationWeatherSeverity.Extreme => -2,
                _ => 0
            };

            if (weather.WindSpeedKph >= 60m)
            {
                healthDeltaPerBlock -= 1;
                happinessDeltaPerBlock -= 1;
            }

            return new ExposureRule(
                StepHours: stepHours,
                HealthDeltaPerBlock: healthDeltaPerBlock,
                HappinessDeltaPerBlock: happinessDeltaPerBlock);
        }

        private static ExposureRule? CreateSnowStressRule(
            WeatherImpactProfile weather,
            bool isSensitive)
        {
            decimal stepHours = weather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 24m,
                PopulationWeatherSeverity.Severe => 12m,
                PopulationWeatherSeverity.Extreme => 8m,
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
                PopulationWeatherSeverity.Severe => -2,
                PopulationWeatherSeverity.Extreme => -2,
                _ => 0
            };

            if (weather.TemperatureC <= -10m)
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

        private static RecoveryRule? CreateTemperatureRecoveryRule(
            WeatherImpactProfile sourceWeather,
            bool comfortableRecoveryWeather,
            bool isSensitive)
        {
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

        private static RecoveryRule? CreateStormRecoveryRule(
            WeatherImpactProfile sourceWeather,
            bool comfortableRecoveryWeather,
            bool isSensitive)
        {
            decimal stepHours = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 12m,
                PopulationWeatherSeverity.Severe => 12m,
                PopulationWeatherSeverity.Extreme => 8m,
                _ => decimal.MaxValue
            };

            if (stepHours == decimal.MaxValue)
                return null;

            int maxBlocks = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Moderate => 1,
                PopulationWeatherSeverity.Severe => 2,
                PopulationWeatherSeverity.Extreme => 2,
                _ => 0
            };

            if (maxBlocks == 0)
                return null;

            int healthDeltaPerBlock = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Extreme when comfortableRecoveryWeather && !isSensitive => 1,
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

        private static RecoveryRule? CreateSnowRecoveryRule(
            WeatherImpactProfile sourceWeather,
            bool comfortableRecoveryWeather,
            bool isSensitive)
        {
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
                PopulationWeatherSeverity.Extreme => 2,
                _ => 0
            };

            if (maxBlocks == 0)
                return null;

            int healthDeltaPerBlock = sourceWeather.Severity switch
            {
                PopulationWeatherSeverity.Extreme when comfortableRecoveryWeather && !isSensitive => 1,
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
