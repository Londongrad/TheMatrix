using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Profiles
{
    /// <summary>
    ///     Seasonal baseline wind profile used by weather evolution services.
    /// </summary>
    public sealed record class SeasonalWindProfile
    {
        private SeasonalWindProfile() { }

        private SeasonalWindProfile(
            WindSpeedKph springAverage,
            WindSpeedKph summerAverage,
            WindSpeedKph autumnAverage,
            WindSpeedKph winterAverage,
            WindSpeedKph gustHeadroom)
        {
            SpringAverage = springAverage;
            SummerAverage = summerAverage;
            AutumnAverage = autumnAverage;
            WinterAverage = winterAverage;
            GustHeadroom = gustHeadroom;
        }

        public WindSpeedKph SpringAverage { get; }
        public WindSpeedKph SummerAverage { get; }
        public WindSpeedKph AutumnAverage { get; }
        public WindSpeedKph WinterAverage { get; }
        public WindSpeedKph GustHeadroom { get; }

        public static SeasonalWindProfile Create(
            WindSpeedKph springAverage,
            WindSpeedKph summerAverage,
            WindSpeedKph autumnAverage,
            WindSpeedKph winterAverage,
            WindSpeedKph gustHeadroom)
        {
            return new SeasonalWindProfile(
                springAverage: springAverage,
                summerAverage: summerAverage,
                autumnAverage: autumnAverage,
                winterAverage: winterAverage,
                gustHeadroom: gustHeadroom);
        }

        public WindSpeedKph GetAverage(WeatherSeason season)
        {
            return season switch
            {
                WeatherSeason.Spring => SpringAverage,
                WeatherSeason.Summer => SummerAverage,
                WeatherSeason.Autumn => AutumnAverage,
                WeatherSeason.Winter => WinterAverage,
                _ => throw ClassicCityDomainErrorsFactory.InvalidClimateProfile(
                    reason: "Unknown weather season for wind profile.",
                    propertyName: nameof(season))
            };
        }
    }
}
