using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Weather.Enums;
using Matrix.CityCore.Domain.Weather.ValueObjects;

namespace Matrix.CityCore.Domain.Weather.Profiles
{
    /// <summary>
    ///     Seasonal baseline temperatures used by the climate profile.
    /// </summary>
    public sealed record class SeasonalTemperatureProfile
    {
        private SeasonalTemperatureProfile() { }

        private SeasonalTemperatureProfile(
            TemperatureC springAverage,
            TemperatureC summerAverage,
            TemperatureC autumnAverage,
            TemperatureC winterAverage,
            TemperatureC dailySwing)
        {
            SpringAverage = springAverage;
            SummerAverage = summerAverage;
            AutumnAverage = autumnAverage;
            WinterAverage = winterAverage;
            DailySwing = dailySwing;
        }

        public TemperatureC SpringAverage { get; }
        public TemperatureC SummerAverage { get; }
        public TemperatureC AutumnAverage { get; }
        public TemperatureC WinterAverage { get; }
        public TemperatureC DailySwing { get; }

        public static SeasonalTemperatureProfile Create(
            TemperatureC springAverage,
            TemperatureC summerAverage,
            TemperatureC autumnAverage,
            TemperatureC winterAverage,
            TemperatureC dailySwing)
        {
            if (dailySwing.Value < 0m)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Daily temperature swing cannot be negative.",
                    propertyName: nameof(dailySwing));

            return new SeasonalTemperatureProfile(
                springAverage: springAverage,
                summerAverage: summerAverage,
                autumnAverage: autumnAverage,
                winterAverage: winterAverage,
                dailySwing: dailySwing);
        }

        public TemperatureC GetAverage(WeatherSeason season)
        {
            return season switch
            {
                WeatherSeason.Spring => SpringAverage,
                WeatherSeason.Summer => SummerAverage,
                WeatherSeason.Autumn => AutumnAverage,
                WeatherSeason.Winter => WinterAverage,
                _ => throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Unknown weather season for temperature profile.",
                    propertyName: nameof(season))
            };
        }
    }
}
