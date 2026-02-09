using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather
{
    /// <summary>
    ///     Long-lived climate baseline for a city.
    /// </summary>
    public sealed record class WeatherClimateProfile
    {
        private WeatherClimateProfile() { }

        private WeatherClimateProfile(
            ClimateZone climateZone,
            SeasonalTemperatureProfile temperatureProfile,
            SeasonalPrecipitationProfile precipitationProfile,
            SeasonalWindProfile windProfile,
            WeatherVolatility volatility,
            ExtremeWeatherProfile extremeWeatherProfile)
        {
            ClimateZone = climateZone;
            TemperatureProfile = temperatureProfile;
            PrecipitationProfile = precipitationProfile;
            WindProfile = windProfile;
            Volatility = volatility;
            ExtremeWeatherProfile = extremeWeatherProfile;
        }

        public ClimateZone ClimateZone { get; }
        public SeasonalTemperatureProfile TemperatureProfile { get; } = null!;
        public SeasonalPrecipitationProfile PrecipitationProfile { get; } = null!;
        public SeasonalWindProfile WindProfile { get; } = null!;
        public WeatherVolatility Volatility { get; }
        public ExtremeWeatherProfile ExtremeWeatherProfile { get; } = null!;

        public static WeatherClimateProfile Create(
            ClimateZone climateZone,
            SeasonalTemperatureProfile temperatureProfile,
            SeasonalPrecipitationProfile precipitationProfile,
            SeasonalWindProfile windProfile,
            WeatherVolatility volatility,
            ExtremeWeatherProfile extremeWeatherProfile)
        {
            GuardHelper.AgainstInvalidEnum(
                value: climateZone,
                propertyName: nameof(ClimateZone));

            if (temperatureProfile is null)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Temperature profile is required.",
                    propertyName: nameof(temperatureProfile));

            if (precipitationProfile is null)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Precipitation profile is required.",
                    propertyName: nameof(precipitationProfile));

            if (windProfile is null)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Wind profile is required.",
                    propertyName: nameof(windProfile));

            if (extremeWeatherProfile is null)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Extreme weather profile is required.",
                    propertyName: nameof(extremeWeatherProfile));

            return new WeatherClimateProfile(
                climateZone: climateZone,
                temperatureProfile: temperatureProfile,
                precipitationProfile: precipitationProfile,
                windProfile: windProfile,
                volatility: volatility,
                extremeWeatherProfile: extremeWeatherProfile);
        }

        public TemperatureC GetBaselineTemperature(WeatherSeason season)
        {
            return TemperatureProfile.GetAverage(season);
        }

        public HumidityPercent GetBaselineHumidity(WeatherSeason season)
        {
            return PrecipitationProfile.GetHumidity(season);
        }

        public PrecipitationKind GetDominantPrecipitation(WeatherSeason season)
        {
            return PrecipitationProfile.GetDominantKind(season);
        }

        public WindSpeedKph GetBaselineWindSpeed(WeatherSeason season)
        {
            return WindProfile.GetAverage(season);
        }
    }
}
