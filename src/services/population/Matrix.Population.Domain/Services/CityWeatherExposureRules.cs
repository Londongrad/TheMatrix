using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Domain.Services
{
    public static class CityWeatherExposureRules
    {
        public static bool IsAdverseExposureWeather(WeatherImpactProfile weather)
        {
            return weather.Type switch
            {
                PopulationWeatherType.Heatwave => weather.Severity >= PopulationWeatherSeverity.Moderate,
                PopulationWeatherType.ColdSnap => weather.Severity >= PopulationWeatherSeverity.Moderate,
                PopulationWeatherType.Storm => weather.Severity >= PopulationWeatherSeverity.Moderate,
                PopulationWeatherType.Snow => weather.Severity >= PopulationWeatherSeverity.Moderate,
                _ => false
            };
        }

        public static bool IsRecoveryWeather(WeatherImpactProfile weather)
        {
            return weather.Type is PopulationWeatherType.Clear or PopulationWeatherType.Overcast &&
                   weather.Severity <= PopulationWeatherSeverity.Mild &&
                   weather.PrecipitationKind == PopulationPrecipitationKind.None &&
                   weather.WindSpeedKph <= 25m &&
                   weather.TemperatureC is >= 10m and <= 28m;
        }

        public static bool IsComfortableRecoveryWeather(WeatherImpactProfile weather)
        {
            return weather.Type == PopulationWeatherType.Clear &&
                   weather.Severity <= PopulationWeatherSeverity.Mild &&
                   weather.TemperatureC is >= 18m and <= 24m &&
                   weather.HumidityPercent is >= 35m and <= 65m &&
                   weather.WindSpeedKph <= 20m &&
                   weather.PrecipitationKind == PopulationPrecipitationKind.None;
        }
    }
}
