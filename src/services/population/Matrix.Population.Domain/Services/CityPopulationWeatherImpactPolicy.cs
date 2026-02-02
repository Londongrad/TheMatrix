using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Domain.Services
{
    public sealed class CityPopulationWeatherImpactPolicy
    {
        public PersonWeatherImpact Calculate(
            Person person,
            DateOnly currentDate,
            WeatherImpactProfile weather)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentNullException.ThrowIfNull(weather);

            if (!person.IsAlive)
                return PersonWeatherImpact.None;

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

            return new PersonWeatherImpact(
                HealthDelta: healthDelta,
                HappinessDelta: happinessDelta);
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
