using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Profiles
{
    /// <summary>
    ///     Upper envelope of extreme conditions allowed for a city's climate.
    /// </summary>
    public sealed record class ExtremeWeatherProfile
    {
        private ExtremeWeatherProfile() { }

        private ExtremeWeatherProfile(
            WeatherSeverity maxOverallSeverity,
            bool supportsThunderstorms,
            bool supportsSnowstorms,
            bool supportsFog,
            bool supportsHeatwaves)
        {
            MaxOverallSeverity = maxOverallSeverity;
            SupportsThunderstorms = supportsThunderstorms;
            SupportsSnowstorms = supportsSnowstorms;
            SupportsFog = supportsFog;
            SupportsHeatwaves = supportsHeatwaves;
        }

        public WeatherSeverity MaxOverallSeverity { get; }
        public bool SupportsThunderstorms { get; }
        public bool SupportsSnowstorms { get; }
        public bool SupportsFog { get; }
        public bool SupportsHeatwaves { get; }

        public static ExtremeWeatherProfile Create(
            WeatherSeverity maxOverallSeverity,
            bool supportsThunderstorms,
            bool supportsSnowstorms,
            bool supportsFog,
            bool supportsHeatwaves)
        {
            GuardHelper.AgainstInvalidEnum(
                value: maxOverallSeverity,
                propertyName: nameof(MaxOverallSeverity));

            bool hasAnyExtremeCapability = supportsThunderstorms ||
                                           supportsSnowstorms ||
                                           supportsFog ||
                                           supportsHeatwaves;

            if (maxOverallSeverity == WeatherSeverity.Calm && hasAnyExtremeCapability)
                throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Extreme weather capabilities require a non-calm maximum severity.",
                    propertyName: nameof(maxOverallSeverity));

            return new ExtremeWeatherProfile(
                maxOverallSeverity: maxOverallSeverity,
                supportsThunderstorms: supportsThunderstorms,
                supportsSnowstorms: supportsSnowstorms,
                supportsFog: supportsFog,
                supportsHeatwaves: supportsHeatwaves);
        }
    }
}
