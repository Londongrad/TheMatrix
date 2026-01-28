using Matrix.CityCore.Application.Services.Weather.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;
using Matrix.CityCore.Domain.Weather.Enums;
using Matrix.CityCore.Domain.Weather.Profiles;
using Matrix.CityCore.Domain.Weather.ValueObjects;

namespace Matrix.CityCore.Application.Services.Weather
{
    /// <summary>
    ///     Creates the initial city weather deterministically from city context and simulation time.
    /// </summary>
    public sealed class CityWeatherBootstrapFactory(IWeatherStatePlanner planner) : ICityWeatherBootstrapFactory
    {
        public CityWeather CreateInitial(
            City city,
            SimTime initialTime)
        {
            ArgumentNullException.ThrowIfNull(city);

            WeatherClimateProfile climateProfile = BuildClimateProfile(city.Environment.ClimateZone);
            WeatherState initialState = planner.PlanNaturalState(
                environment: city.Environment,
                climateProfile: climateProfile,
                evaluatedAt: initialTime);

            return CityWeather.Create(
                cityId: city.Id,
                climateProfile: climateProfile,
                currentState: initialState,
                createdAt: initialTime);
        }

        private static WeatherClimateProfile BuildClimateProfile(ClimateZone climateZone)
        {
            return climateZone switch
            {
                ClimateZone.Tropical => WeatherClimateProfile.Create(
                    climateZone: climateZone,
                    temperatureProfile: SeasonalTemperatureProfile.Create(
                        springAverage: TemperatureC.From(28m),
                        summerAverage: TemperatureC.From(31m),
                        autumnAverage: TemperatureC.From(29m),
                        winterAverage: TemperatureC.From(27m),
                        dailySwing: TemperatureC.From(6m)),
                    precipitationProfile: SeasonalPrecipitationProfile.Create(
                        springHumidity: HumidityPercent.From(78m),
                        summerHumidity: HumidityPercent.From(82m),
                        autumnHumidity: HumidityPercent.From(80m),
                        winterHumidity: HumidityPercent.From(77m),
                        springDominantKind: PrecipitationKind.Rain,
                        summerDominantKind: PrecipitationKind.Rain,
                        autumnDominantKind: PrecipitationKind.Rain,
                        winterDominantKind: PrecipitationKind.Rain),
                    windProfile: SeasonalWindProfile.Create(
                        springAverage: WindSpeedKph.From(12m),
                        summerAverage: WindSpeedKph.From(14m),
                        autumnAverage: WindSpeedKph.From(13m),
                        winterAverage: WindSpeedKph.From(11m),
                        gustHeadroom: WindSpeedKph.From(28m)),
                    volatility: WeatherVolatility.From(0.55m),
                    extremeWeatherProfile: ExtremeWeatherProfile.Create(
                        maxOverallSeverity: WeatherSeverity.Severe,
                        supportsThunderstorms: true,
                        supportsSnowstorms: false,
                        supportsFog: false,
                        supportsHeatwaves: true)),
                ClimateZone.Temperate => WeatherClimateProfile.Create(
                    climateZone: climateZone,
                    temperatureProfile: SeasonalTemperatureProfile.Create(
                        springAverage: TemperatureC.From(12m),
                        summerAverage: TemperatureC.From(24m),
                        autumnAverage: TemperatureC.From(11m),
                        winterAverage: TemperatureC.From(2m),
                        dailySwing: TemperatureC.From(9m)),
                    precipitationProfile: SeasonalPrecipitationProfile.Create(
                        springHumidity: HumidityPercent.From(64m),
                        summerHumidity: HumidityPercent.From(58m),
                        autumnHumidity: HumidityPercent.From(72m),
                        winterHumidity: HumidityPercent.From(76m),
                        springDominantKind: PrecipitationKind.Rain,
                        summerDominantKind: PrecipitationKind.Drizzle,
                        autumnDominantKind: PrecipitationKind.Rain,
                        winterDominantKind: PrecipitationKind.Sleet),
                    windProfile: SeasonalWindProfile.Create(
                        springAverage: WindSpeedKph.From(14m),
                        summerAverage: WindSpeedKph.From(12m),
                        autumnAverage: WindSpeedKph.From(18m),
                        winterAverage: WindSpeedKph.From(20m),
                        gustHeadroom: WindSpeedKph.From(32m)),
                    volatility: WeatherVolatility.From(0.36m),
                    extremeWeatherProfile: ExtremeWeatherProfile.Create(
                        maxOverallSeverity: WeatherSeverity.Severe,
                        supportsThunderstorms: true,
                        supportsSnowstorms: true,
                        supportsFog: true,
                        supportsHeatwaves: true)),
                ClimateZone.Continental => WeatherClimateProfile.Create(
                    climateZone: climateZone,
                    temperatureProfile: SeasonalTemperatureProfile.Create(
                        springAverage: TemperatureC.From(10m),
                        summerAverage: TemperatureC.From(26m),
                        autumnAverage: TemperatureC.From(8m),
                        winterAverage: TemperatureC.From(-8m),
                        dailySwing: TemperatureC.From(12m)),
                    precipitationProfile: SeasonalPrecipitationProfile.Create(
                        springHumidity: HumidityPercent.From(60m),
                        summerHumidity: HumidityPercent.From(56m),
                        autumnHumidity: HumidityPercent.From(67m),
                        winterHumidity: HumidityPercent.From(74m),
                        springDominantKind: PrecipitationKind.Rain,
                        summerDominantKind: PrecipitationKind.Drizzle,
                        autumnDominantKind: PrecipitationKind.Rain,
                        winterDominantKind: PrecipitationKind.Snow),
                    windProfile: SeasonalWindProfile.Create(
                        springAverage: WindSpeedKph.From(15m),
                        summerAverage: WindSpeedKph.From(13m),
                        autumnAverage: WindSpeedKph.From(19m),
                        winterAverage: WindSpeedKph.From(24m),
                        gustHeadroom: WindSpeedKph.From(38m)),
                    volatility: WeatherVolatility.From(0.44m),
                    extremeWeatherProfile: ExtremeWeatherProfile.Create(
                        maxOverallSeverity: WeatherSeverity.Extreme,
                        supportsThunderstorms: true,
                        supportsSnowstorms: true,
                        supportsFog: true,
                        supportsHeatwaves: true)),
                ClimateZone.Arid => WeatherClimateProfile.Create(
                    climateZone: climateZone,
                    temperatureProfile: SeasonalTemperatureProfile.Create(
                        springAverage: TemperatureC.From(22m),
                        summerAverage: TemperatureC.From(34m),
                        autumnAverage: TemperatureC.From(20m),
                        winterAverage: TemperatureC.From(11m),
                        dailySwing: TemperatureC.From(13m)),
                    precipitationProfile: SeasonalPrecipitationProfile.Create(
                        springHumidity: HumidityPercent.From(32m),
                        summerHumidity: HumidityPercent.From(21m),
                        autumnHumidity: HumidityPercent.From(34m),
                        winterHumidity: HumidityPercent.From(42m),
                        springDominantKind: PrecipitationKind.None,
                        summerDominantKind: PrecipitationKind.None,
                        autumnDominantKind: PrecipitationKind.Drizzle,
                        winterDominantKind: PrecipitationKind.Rain),
                    windProfile: SeasonalWindProfile.Create(
                        springAverage: WindSpeedKph.From(18m),
                        summerAverage: WindSpeedKph.From(22m),
                        autumnAverage: WindSpeedKph.From(17m),
                        winterAverage: WindSpeedKph.From(14m),
                        gustHeadroom: WindSpeedKph.From(35m)),
                    volatility: WeatherVolatility.From(0.25m),
                    extremeWeatherProfile: ExtremeWeatherProfile.Create(
                        maxOverallSeverity: WeatherSeverity.Severe,
                        supportsThunderstorms: false,
                        supportsSnowstorms: false,
                        supportsFog: false,
                        supportsHeatwaves: true)),
                ClimateZone.Polar => WeatherClimateProfile.Create(
                    climateZone: climateZone,
                    temperatureProfile: SeasonalTemperatureProfile.Create(
                        springAverage: TemperatureC.From(-12m),
                        summerAverage: TemperatureC.From(2m),
                        autumnAverage: TemperatureC.From(-10m),
                        winterAverage: TemperatureC.From(-24m),
                        dailySwing: TemperatureC.From(7m)),
                    precipitationProfile: SeasonalPrecipitationProfile.Create(
                        springHumidity: HumidityPercent.From(56m),
                        summerHumidity: HumidityPercent.From(60m),
                        autumnHumidity: HumidityPercent.From(58m),
                        winterHumidity: HumidityPercent.From(66m),
                        springDominantKind: PrecipitationKind.Snow,
                        summerDominantKind: PrecipitationKind.Sleet,
                        autumnDominantKind: PrecipitationKind.Snow,
                        winterDominantKind: PrecipitationKind.Snow),
                    windProfile: SeasonalWindProfile.Create(
                        springAverage: WindSpeedKph.From(22m),
                        summerAverage: WindSpeedKph.From(18m),
                        autumnAverage: WindSpeedKph.From(24m),
                        winterAverage: WindSpeedKph.From(28m),
                        gustHeadroom: WindSpeedKph.From(45m)),
                    volatility: WeatherVolatility.From(0.46m),
                    extremeWeatherProfile: ExtremeWeatherProfile.Create(
                        maxOverallSeverity: WeatherSeverity.Extreme,
                        supportsThunderstorms: false,
                        supportsSnowstorms: true,
                        supportsFog: true,
                        supportsHeatwaves: false)),
                ClimateZone.Mountain => WeatherClimateProfile.Create(
                    climateZone: climateZone,
                    temperatureProfile: SeasonalTemperatureProfile.Create(
                        springAverage: TemperatureC.From(6m),
                        summerAverage: TemperatureC.From(16m),
                        autumnAverage: TemperatureC.From(5m),
                        winterAverage: TemperatureC.From(-10m),
                        dailySwing: TemperatureC.From(11m)),
                    precipitationProfile: SeasonalPrecipitationProfile.Create(
                        springHumidity: HumidityPercent.From(68m),
                        summerHumidity: HumidityPercent.From(60m),
                        autumnHumidity: HumidityPercent.From(71m),
                        winterHumidity: HumidityPercent.From(78m),
                        springDominantKind: PrecipitationKind.Sleet,
                        summerDominantKind: PrecipitationKind.Drizzle,
                        autumnDominantKind: PrecipitationKind.Rain,
                        winterDominantKind: PrecipitationKind.Snow),
                    windProfile: SeasonalWindProfile.Create(
                        springAverage: WindSpeedKph.From(20m),
                        summerAverage: WindSpeedKph.From(17m),
                        autumnAverage: WindSpeedKph.From(23m),
                        winterAverage: WindSpeedKph.From(27m),
                        gustHeadroom: WindSpeedKph.From(42m)),
                    volatility: WeatherVolatility.From(0.5m),
                    extremeWeatherProfile: ExtremeWeatherProfile.Create(
                        maxOverallSeverity: WeatherSeverity.Extreme,
                        supportsThunderstorms: true,
                        supportsSnowstorms: true,
                        supportsFog: true,
                        supportsHeatwaves: false)),
                _ => throw new ArgumentOutOfRangeException(nameof(climateZone), climateZone, null)
            };
        }
    }
}
