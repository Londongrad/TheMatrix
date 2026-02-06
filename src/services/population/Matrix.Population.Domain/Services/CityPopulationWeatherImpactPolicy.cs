using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Domain.Services
{
    public sealed class CityPopulationWeatherImpactPolicy(
        CityPopulationClimateAdaptationPolicy climateAdaptationPolicy)
    {
        public PersonWeatherImpact CalculateDifferential(
            Person person,
            DateOnly currentDate,
            WeatherImpactProfile previousWeather,
            WeatherImpactProfile currentWeather,
            CityPopulationEnvironment? environment)
        {
            person = GuardHelper.AgainstNull(
                value: person,
                propertyName: nameof(person));
            previousWeather = GuardHelper.AgainstNull(
                value: previousWeather,
                propertyName: nameof(previousWeather));
            currentWeather = GuardHelper.AgainstNull(
                value: currentWeather,
                propertyName: nameof(currentWeather));

            if (!person.IsAlive)
                return PersonWeatherImpact.None;

            PersonWeatherImpact previousImpact = CalculateSnapshot(
                person: person,
                currentDate: currentDate,
                weather: previousWeather,
                environment: environment,
                climateAdaptationPolicy: climateAdaptationPolicy);
            PersonWeatherImpact currentImpact = CalculateSnapshot(
                person: person,
                currentDate: currentDate,
                weather: currentWeather,
                environment: environment,
                climateAdaptationPolicy: climateAdaptationPolicy);

            int healthDelta = currentImpact.HealthDelta - previousImpact.HealthDelta;
            int happinessDelta = currentImpact.HappinessDelta - previousImpact.HappinessDelta;

            // Weather changes can worsen health immediately, but improvement should not
            // magically heal people in a single transition event.
            healthDelta = Math.Min(0, healthDelta);

            int severityShift = GetSeverityRank(currentWeather.Severity) - GetSeverityRank(previousWeather.Severity);
            if (severityShift >= 2)
                happinessDelta -= 1;
            else if (severityShift <= -2)
                happinessDelta += 1;

            AgeGroup ageGroup = person.GetAgeGroup(currentDate);

            if (IsAbruptAdverseOnset(previousWeather, currentWeather))
            {
                happinessDelta -= 1;

                if (IsWeatherSensitiveAgeGroup(ageGroup))
                    healthDelta -= 1;
            }

            if (IsReliefTransition(previousWeather, currentWeather))
                happinessDelta += 1;

            return new PersonWeatherImpact(
                HealthDelta: Math.Clamp(healthDelta, min: -4, max: 0),
                HappinessDelta: Math.Clamp(happinessDelta, min: -6, max: 4));
        }

        private static PersonWeatherImpact CalculateSnapshot(
            Person person,
            DateOnly currentDate,
            WeatherImpactProfile weather,
            CityPopulationEnvironment? environment,
            CityPopulationClimateAdaptationPolicy climateAdaptationPolicy)
        {
            int healthDelta = GetBaseHealthDelta(weather);
            int happinessDelta = GetBaseHappinessDelta(weather);

            if (weather.Type == PopulationWeatherType.Clear && IsComfortableClearWeather(weather))
                happinessDelta += 1;

            if ((weather.Type is PopulationWeatherType.Heatwave or PopulationWeatherType.ColdSnap) &&
                IsExtremeTemperature(weather))
            {
                healthDelta -= 1;
                happinessDelta -= 1;
            }

            if (weather.Type == PopulationWeatherType.Storm && weather.WindSpeedKph >= 60m)
            {
                healthDelta -= 1;
                happinessDelta -= 1;
            }

            if (weather.PrecipitationKind is PopulationPrecipitationKind.Hail or PopulationPrecipitationKind.Sleet &&
                weather.Severity >= PopulationWeatherSeverity.Moderate)
            {
                healthDelta -= 1;
                happinessDelta -= 1;
            }

            if (weather.Type == PopulationWeatherType.Fog &&
                weather.Severity >= PopulationWeatherSeverity.Moderate &&
                weather.CloudCoveragePercent >= 85m)
                happinessDelta -= 1;

            AgeGroup ageGroup = person.GetAgeGroup(currentDate);
            if (IsWeatherSensitiveAgeGroup(ageGroup) &&
                weather.Severity >= PopulationWeatherSeverity.Moderate &&
                (weather.Type is PopulationWeatherType.Storm
                    or PopulationWeatherType.Snow
                    or PopulationWeatherType.Windy
                    or PopulationWeatherType.Heatwave
                    or PopulationWeatherType.ColdSnap))
                healthDelta -= 1;

            int toleranceScore = climateAdaptationPolicy.GetToleranceScore(
                environment: environment,
                currentDate: currentDate,
                weatherType: weather.Type);

            healthDelta = climateAdaptationPolicy.AdjustNegativeDelta(
                delta: healthDelta,
                toleranceScore: toleranceScore);
            happinessDelta = climateAdaptationPolicy.AdjustNegativeDelta(
                delta: happinessDelta,
                toleranceScore: toleranceScore);

            return new PersonWeatherImpact(
                HealthDelta: healthDelta,
                HappinessDelta: happinessDelta);
        }

        private static bool IsAbruptAdverseOnset(
            WeatherImpactProfile previousWeather,
            WeatherImpactProfile currentWeather)
        {
            return !IsAdverseWeather(previousWeather) &&
                   IsAdverseWeather(currentWeather) &&
                   currentWeather.Severity >= PopulationWeatherSeverity.Moderate;
        }

        private static bool IsReliefTransition(
            WeatherImpactProfile previousWeather,
            WeatherImpactProfile currentWeather)
        {
            return IsAdverseWeather(previousWeather) &&
                   previousWeather.Severity >= PopulationWeatherSeverity.Moderate &&
                   (!IsAdverseWeather(currentWeather) || GetSeverityRank(currentWeather.Severity) < GetSeverityRank(previousWeather.Severity));
        }

        private static bool IsAdverseWeather(WeatherImpactProfile weather)
        {
            return weather.Type is PopulationWeatherType.Rain
                or PopulationWeatherType.Snow
                or PopulationWeatherType.Storm
                or PopulationWeatherType.Fog
                or PopulationWeatherType.Windy
                or PopulationWeatherType.Heatwave
                or PopulationWeatherType.ColdSnap;
        }

        private static int GetSeverityRank(PopulationWeatherSeverity severity)
        {
            return severity switch
            {
                PopulationWeatherSeverity.Calm => 0,
                PopulationWeatherSeverity.Mild => 1,
                PopulationWeatherSeverity.Moderate => 2,
                PopulationWeatherSeverity.Severe => 3,
                PopulationWeatherSeverity.Extreme => 4,
                _ => -1
            };
        }

        private static int GetBaseHealthDelta(WeatherImpactProfile weather)
        {
            return weather.Type switch
            {
                PopulationWeatherType.Rain => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => 0,
                    PopulationWeatherSeverity.Moderate => -1,
                    PopulationWeatherSeverity.Severe => -1,
                    PopulationWeatherSeverity.Extreme => -2,
                    _ => 0
                },
                PopulationWeatherType.Snow => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => 0,
                    PopulationWeatherSeverity.Moderate => -1,
                    PopulationWeatherSeverity.Severe => -2,
                    PopulationWeatherSeverity.Extreme => -3,
                    _ => 0
                },
                PopulationWeatherType.Storm => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => -1,
                    PopulationWeatherSeverity.Mild => -1,
                    PopulationWeatherSeverity.Moderate => -2,
                    PopulationWeatherSeverity.Severe => -3,
                    PopulationWeatherSeverity.Extreme => -4,
                    _ => 0
                },
                PopulationWeatherType.Fog => weather.Severity switch
                {
                    PopulationWeatherSeverity.Severe => -1,
                    PopulationWeatherSeverity.Extreme => -1,
                    _ => 0
                },
                PopulationWeatherType.Windy => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => 0,
                    PopulationWeatherSeverity.Moderate => -1,
                    PopulationWeatherSeverity.Severe => -1,
                    PopulationWeatherSeverity.Extreme => -2,
                    _ => 0
                },
                PopulationWeatherType.Heatwave => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => -1,
                    PopulationWeatherSeverity.Moderate => -2,
                    PopulationWeatherSeverity.Severe => -3,
                    PopulationWeatherSeverity.Extreme => -4,
                    _ => 0
                },
                PopulationWeatherType.ColdSnap => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => -1,
                    PopulationWeatherSeverity.Moderate => -2,
                    PopulationWeatherSeverity.Severe => -3,
                    PopulationWeatherSeverity.Extreme => -4,
                    _ => 0
                },
                _ => 0
            };
        }

        private static int GetBaseHappinessDelta(WeatherImpactProfile weather)
        {
            return weather.Type switch
            {
                PopulationWeatherType.Clear => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 2,
                    PopulationWeatherSeverity.Mild => 1,
                    PopulationWeatherSeverity.Moderate => 1,
                    PopulationWeatherSeverity.Severe => 0,
                    PopulationWeatherSeverity.Extreme => -1,
                    _ => 0
                },
                PopulationWeatherType.Overcast => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => -1,
                    PopulationWeatherSeverity.Moderate => -1,
                    PopulationWeatherSeverity.Severe => -2,
                    PopulationWeatherSeverity.Extreme => -2,
                    _ => 0
                },
                PopulationWeatherType.Rain => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => -1,
                    PopulationWeatherSeverity.Moderate => -2,
                    PopulationWeatherSeverity.Severe => -3,
                    PopulationWeatherSeverity.Extreme => -4,
                    _ => 0
                },
                PopulationWeatherType.Snow => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => 0,
                    PopulationWeatherSeverity.Moderate => -1,
                    PopulationWeatherSeverity.Severe => -2,
                    PopulationWeatherSeverity.Extreme => -3,
                    _ => 0
                },
                PopulationWeatherType.Storm => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => -1,
                    PopulationWeatherSeverity.Mild => -2,
                    PopulationWeatherSeverity.Moderate => -3,
                    PopulationWeatherSeverity.Severe => -4,
                    PopulationWeatherSeverity.Extreme => -5,
                    _ => 0
                },
                PopulationWeatherType.Fog => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => -1,
                    PopulationWeatherSeverity.Moderate => -1,
                    PopulationWeatherSeverity.Severe => -2,
                    PopulationWeatherSeverity.Extreme => -3,
                    _ => 0
                },
                PopulationWeatherType.Windy => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => 0,
                    PopulationWeatherSeverity.Mild => -1,
                    PopulationWeatherSeverity.Moderate => -1,
                    PopulationWeatherSeverity.Severe => -2,
                    PopulationWeatherSeverity.Extreme => -3,
                    _ => 0
                },
                PopulationWeatherType.Heatwave => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => -1,
                    PopulationWeatherSeverity.Mild => -2,
                    PopulationWeatherSeverity.Moderate => -3,
                    PopulationWeatherSeverity.Severe => -4,
                    PopulationWeatherSeverity.Extreme => -5,
                    _ => 0
                },
                PopulationWeatherType.ColdSnap => weather.Severity switch
                {
                    PopulationWeatherSeverity.Calm => -1,
                    PopulationWeatherSeverity.Mild => -2,
                    PopulationWeatherSeverity.Moderate => -3,
                    PopulationWeatherSeverity.Severe => -4,
                    PopulationWeatherSeverity.Extreme => -5,
                    _ => 0
                },
                _ => 0
            };
        }

        private static bool IsComfortableClearWeather(WeatherImpactProfile weather)
        {
            return weather.TemperatureC is >= 18m and <= 24m &&
                   weather.HumidityPercent is >= 35m and <= 65m &&
                   weather.WindSpeedKph <= 20m &&
                   weather.PrecipitationKind == PopulationPrecipitationKind.None;
        }

        private static bool IsExtremeTemperature(WeatherImpactProfile weather)
        {
            return weather.TemperatureC >= 35m || weather.TemperatureC <= -15m;
        }

        private static bool IsWeatherSensitiveAgeGroup(AgeGroup ageGroup)
        {
            return ageGroup is AgeGroup.Child or AgeGroup.Senior;
        }
    }
}
