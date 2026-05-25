using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather
{
    public sealed class WeatherClimateProfileTests
    {
        private const string InvalidClimateProfileCode = "SimulationCore.Weather.ClimateProfile.Invalid";
        private const string InvalidEnumCode = "Domain.Guard.InvalidEnum";

        [Fact]
        public void SeasonalTemperatureProfile_Create_WithNegativeDailySwing_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => SeasonalTemperatureProfile.Create(
                springAverage: TemperatureC.From(12m),
                summerAverage: TemperatureC.From(24m),
                autumnAverage: TemperatureC.From(10m),
                winterAverage: TemperatureC.From(-6m),
                dailySwing: TemperatureC.From(-1m)));

            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "dailySwing",
                actual: exception.PropertyName);
        }

        [Fact]
        public void SeasonalTemperatureProfile_GetAverage_ReturnsSeasonalValues_AndRejectsInvalidSeason()
        {
            SeasonalTemperatureProfile profile = WeatherTestData.CreateTemperatureProfile();

            Assert.Equal(
                expected: profile.SpringAverage,
                actual: profile.GetAverage(WeatherSeason.Spring));
            Assert.Equal(
                expected: profile.SummerAverage,
                actual: profile.GetAverage(WeatherSeason.Summer));
            Assert.Equal(
                expected: profile.AutumnAverage,
                actual: profile.GetAverage(WeatherSeason.Autumn));
            Assert.Equal(
                expected: profile.WinterAverage,
                actual: profile.GetAverage(WeatherSeason.Winter));

            DomainException exception = Assert.Throws<DomainException>(() => profile.GetAverage((WeatherSeason)999));

            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "season",
                actual: exception.PropertyName);
        }

        [Fact]
        public void SeasonalPrecipitationProfile_Create_WithInvalidKind_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => SeasonalPrecipitationProfile.Create(
                springHumidity: HumidityPercent.From(58m),
                summerHumidity: HumidityPercent.From(62m),
                autumnHumidity: HumidityPercent.From(70m),
                winterHumidity: HumidityPercent.From(77m),
                springDominantKind: (PrecipitationKind)999,
                summerDominantKind: PrecipitationKind.Rain,
                autumnDominantKind: PrecipitationKind.Drizzle,
                winterDominantKind: PrecipitationKind.Snow));

            Assert.Equal(
                expected: InvalidEnumCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "SpringDominantKind",
                actual: exception.PropertyName);
        }

        [Fact]
        public void SeasonalPrecipitationProfile_GetMembers_ReturnSeasonalValues_AndRejectInvalidSeason()
        {
            SeasonalPrecipitationProfile profile = WeatherTestData.CreatePrecipitationProfile();

            Assert.Equal(
                expected: profile.SpringHumidity,
                actual: profile.GetHumidity(WeatherSeason.Spring));
            Assert.Equal(
                expected: profile.WinterHumidity,
                actual: profile.GetHumidity(WeatherSeason.Winter));
            Assert.Equal(
                expected: profile.SummerDominantKind,
                actual: profile.GetDominantKind(WeatherSeason.Summer));
            Assert.Equal(
                expected: profile.AutumnDominantKind,
                actual: profile.GetDominantKind(WeatherSeason.Autumn));

            DomainException humidityException =
                Assert.Throws<DomainException>(() => profile.GetHumidity((WeatherSeason)999));
            DomainException kindException =
                Assert.Throws<DomainException>(() => profile.GetDominantKind((WeatherSeason)999));

            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: humidityException.Code);
            Assert.Equal(
                expected: "season",
                actual: humidityException.PropertyName);
            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: kindException.Code);
            Assert.Equal(
                expected: "season",
                actual: kindException.PropertyName);
        }

        [Fact]
        public void SeasonalWindProfile_GetAverage_ReturnsSeasonalValues_AndRejectsInvalidSeason()
        {
            SeasonalWindProfile profile = WeatherTestData.CreateWindProfile();

            Assert.Equal(
                expected: profile.SpringAverage,
                actual: profile.GetAverage(WeatherSeason.Spring));
            Assert.Equal(
                expected: profile.WinterAverage,
                actual: profile.GetAverage(WeatherSeason.Winter));

            DomainException exception = Assert.Throws<DomainException>(() => profile.GetAverage((WeatherSeason)999));

            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "season",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ExtremeWeatherProfile_Create_WithCalmSeverityAndCapabilities_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => ExtremeWeatherProfile.Create(
                maxOverallSeverity: WeatherSeverity.Calm,
                supportsThunderstorms: true,
                supportsSnowstorms: false,
                supportsFog: false,
                supportsHeatwaves: false));

            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "maxOverallSeverity",
                actual: exception.PropertyName);
        }

        [Fact]
        public void ExtremeWeatherProfile_Create_WithInvalidSeverity_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => ExtremeWeatherProfile.Create(
                maxOverallSeverity: (WeatherSeverity)999,
                supportsThunderstorms: false,
                supportsSnowstorms: false,
                supportsFog: false,
                supportsHeatwaves: false));

            Assert.Equal(
                expected: InvalidEnumCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "MaxOverallSeverity",
                actual: exception.PropertyName);
        }

        [Fact]
        public void WeatherClimateProfile_Create_WithValidProfiles_CreatesClimateProfile_AndReturnsBaselines()
        {
            SeasonalTemperatureProfile temperatureProfile = WeatherTestData.CreateTemperatureProfile();
            SeasonalPrecipitationProfile precipitationProfile = WeatherTestData.CreatePrecipitationProfile();
            SeasonalWindProfile windProfile = WeatherTestData.CreateWindProfile();
            var volatility = WeatherVolatility.From(0.25m);
            ExtremeWeatherProfile extremeWeatherProfile = WeatherTestData.CreateExtremeWeatherProfile();

            var profile = WeatherClimateProfile.Create(
                climateZone: ClimateZone.Temperate,
                temperatureProfile: temperatureProfile,
                precipitationProfile: precipitationProfile,
                windProfile: windProfile,
                volatility: volatility,
                extremeWeatherProfile: extremeWeatherProfile);

            Assert.Equal(
                expected: ClimateZone.Temperate,
                actual: profile.ClimateZone);
            Assert.Equal(
                expected: temperatureProfile,
                actual: profile.TemperatureProfile);
            Assert.Equal(
                expected: precipitationProfile,
                actual: profile.PrecipitationProfile);
            Assert.Equal(
                expected: windProfile,
                actual: profile.WindProfile);
            Assert.Equal(
                expected: volatility,
                actual: profile.Volatility);
            Assert.Equal(
                expected: extremeWeatherProfile,
                actual: profile.ExtremeWeatherProfile);
            Assert.Equal(
                expected: temperatureProfile.GetAverage(WeatherSeason.Summer),
                actual: profile.GetBaselineTemperature(WeatherSeason.Summer));
            Assert.Equal(
                expected: precipitationProfile.GetHumidity(WeatherSeason.Winter),
                actual: profile.GetBaselineHumidity(WeatherSeason.Winter));
            Assert.Equal(
                expected: precipitationProfile.GetDominantKind(WeatherSeason.Spring),
                actual: profile.GetDominantPrecipitation(WeatherSeason.Spring));
            Assert.Equal(
                expected: windProfile.GetAverage(WeatherSeason.Autumn),
                actual: profile.GetBaselineWindSpeed(WeatherSeason.Autumn));
        }

        [Fact]
        public void WeatherClimateProfile_Create_WithInvalidClimateZone_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WeatherClimateProfile.Create(
                climateZone: (ClimateZone)999,
                temperatureProfile: WeatherTestData.CreateTemperatureProfile(),
                precipitationProfile: WeatherTestData.CreatePrecipitationProfile(),
                windProfile: WeatherTestData.CreateWindProfile(),
                volatility: WeatherVolatility.From(0.25m),
                extremeWeatherProfile: WeatherTestData.CreateExtremeWeatherProfile()));

            Assert.Equal(
                expected: InvalidEnumCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "ClimateZone",
                actual: exception.PropertyName);
        }

        [Fact]
        public void WeatherClimateProfile_Create_WithMissingTemperatureProfile_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => WeatherClimateProfile.Create(
                climateZone: ClimateZone.Temperate,
                temperatureProfile: null!,
                precipitationProfile: WeatherTestData.CreatePrecipitationProfile(),
                windProfile: WeatherTestData.CreateWindProfile(),
                volatility: WeatherVolatility.From(0.25m),
                extremeWeatherProfile: WeatherTestData.CreateExtremeWeatherProfile()));

            Assert.Equal(
                expected: InvalidClimateProfileCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "temperatureProfile",
                actual: exception.PropertyName);
        }
    }
}
