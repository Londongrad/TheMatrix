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
    public sealed class CityWeatherBootstrapFactory : ICityWeatherBootstrapFactory
    {
        public CityWeather CreateInitial(
            City city,
            SimTime initialTime)
        {
            ArgumentNullException.ThrowIfNull(city);

            WeatherClimateProfile climateProfile = BuildClimateProfile(city.Environment.ClimateZone);
            DateTimeOffset localTime = initialTime.ValueUtc.ToOffset(city.Environment.UtcOffset.Value);
            WeatherSeason season = ResolveSeason(
                month: localTime.Month,
                hemisphere: city.Environment.Hemisphere);

            WeatherState initialState = BuildInitialState(
                climateProfile: climateProfile,
                season: season,
                localTime: localTime,
                startedAt: initialTime);

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

        private static WeatherState BuildInitialState(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            DateTimeOffset localTime,
            SimTime startedAt)
        {
            TemperatureC temperature = CalculateTemperature(
                climateProfile: climateProfile,
                season: season,
                hour: localTime.Hour);

            HumidityPercent humidity = CalculateHumidity(
                climateProfile: climateProfile,
                season: season,
                hour: localTime.Hour);

            WindSpeedKph windSpeed = CalculateWindSpeed(
                climateProfile: climateProfile,
                season: season,
                hour: localTime.Hour);

            PrecipitationKind precipitationKind = DeterminePrecipitationKind(
                climateProfile: climateProfile,
                season: season,
                temperature: temperature,
                hour: localTime.Hour);

            WeatherType weatherType = DetermineWeatherType(
                climateProfile: climateProfile,
                temperature: temperature,
                humidity: humidity,
                windSpeed: windSpeed,
                precipitationKind: precipitationKind,
                hour: localTime.Hour);

            WeatherSeverity severity = DetermineSeverity(
                climateProfile: climateProfile,
                weatherType: weatherType,
                precipitationKind: precipitationKind,
                windSpeed: windSpeed);

            CloudCoveragePercent cloudCoverage = DetermineCloudCoverage(
                weatherType: weatherType,
                humidity: humidity,
                precipitationKind: precipitationKind);

            PressureHpa pressure = DeterminePressure(weatherType);
            SimTime expectedUntil = ResolveExpectedUntil(
                startedAt: startedAt,
                localTime: localTime);

            return WeatherState.Create(
                type: weatherType,
                severity: severity,
                precipitationKind: precipitationKind,
                temperature: temperature,
                humidity: humidity,
                windSpeed: windSpeed,
                cloudCoverage: cloudCoverage,
                pressure: pressure,
                startedAt: startedAt,
                expectedUntil: expectedUntil);
        }

        private static TemperatureC CalculateTemperature(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            int hour)
        {
            decimal baseline = climateProfile.GetBaselineTemperature(season).Value;
            decimal swing = climateProfile.TemperatureProfile.DailySwing.Value;
            decimal multiplier = hour switch
            {
                >= 6 and < 12 => 0.2m,
                >= 12 and < 17 => 0.45m,
                >= 17 and < 21 => -0.05m,
                _ => -0.45m
            };

            return TemperatureC.From(Math.Round(baseline + swing * multiplier, 2));
        }

        private static HumidityPercent CalculateHumidity(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            int hour)
        {
            decimal baseline = climateProfile.GetBaselineHumidity(season).Value;
            decimal adjustment = hour switch
            {
                >= 11 and < 17 => -6m,
                >= 17 and < 22 => 2m,
                _ => 6m
            };

            decimal value = Math.Clamp(baseline + adjustment, HumidityPercent.Min, HumidityPercent.Max);
            return HumidityPercent.From(Math.Round(value, 2));
        }

        private static WindSpeedKph CalculateWindSpeed(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            int hour)
        {
            decimal baseline = climateProfile.GetBaselineWindSpeed(season).Value;
            decimal adjustment = hour switch
            {
                >= 10 and < 18 => 4m,
                >= 18 and < 22 => 1m,
                _ => -1m
            };

            decimal value = Math.Clamp(baseline + adjustment, WindSpeedKph.Min, WindSpeedKph.Max);
            return WindSpeedKph.From(Math.Round(value, 2));
        }

        private static PrecipitationKind DeterminePrecipitationKind(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            TemperatureC temperature,
            int hour)
        {
            PrecipitationKind dominantKind = climateProfile.GetDominantPrecipitation(season);

            return dominantKind switch
            {
                PrecipitationKind.None => PrecipitationKind.None,
                PrecipitationKind.Drizzle => PrecipitationKind.Drizzle,
                PrecipitationKind.Rain => hour >= 4 && hour < 10
                    ? PrecipitationKind.Drizzle
                    : PrecipitationKind.Rain,
                PrecipitationKind.Snow => temperature.Value > 0m
                    ? PrecipitationKind.Sleet
                    : PrecipitationKind.Snow,
                PrecipitationKind.Sleet => temperature.Value <= 0m
                    ? PrecipitationKind.Snow
                    : PrecipitationKind.Sleet,
                PrecipitationKind.Hail => PrecipitationKind.Hail,
                _ => PrecipitationKind.None
            };
        }

        private static WeatherType DetermineWeatherType(
            WeatherClimateProfile climateProfile,
            TemperatureC temperature,
            HumidityPercent humidity,
            WindSpeedKph windSpeed,
            PrecipitationKind precipitationKind,
            int hour)
        {
            if (precipitationKind == PrecipitationKind.Hail)
                return WeatherType.Storm;

            if (precipitationKind == PrecipitationKind.Rain || precipitationKind == PrecipitationKind.Drizzle)
                return WeatherType.Rain;

            if (precipitationKind == PrecipitationKind.Snow || precipitationKind == PrecipitationKind.Sleet)
                return WeatherType.Snow;

            if (climateProfile.ExtremeWeatherProfile.SupportsHeatwaves && temperature.Value >= 33m)
                return WeatherType.Heatwave;

            if (temperature.Value <= -18m)
                return WeatherType.ColdSnap;

            if (climateProfile.ExtremeWeatherProfile.SupportsFog &&
                humidity.Value >= 90m &&
                (hour < 8 || hour >= 21))
                return WeatherType.Fog;

            if (windSpeed.Value >= 35m)
                return WeatherType.Windy;

            if (humidity.Value >= 68m)
                return WeatherType.Overcast;

            return WeatherType.Clear;
        }

        private static WeatherSeverity DetermineSeverity(
            WeatherClimateProfile climateProfile,
            WeatherType weatherType,
            PrecipitationKind precipitationKind,
            WindSpeedKph windSpeed)
        {
            WeatherSeverity requested = weatherType switch
            {
                WeatherType.Clear => windSpeed.Value >= 20m
                    ? WeatherSeverity.Mild
                    : WeatherSeverity.Calm,
                WeatherType.Overcast => WeatherSeverity.Mild,
                WeatherType.Fog => WeatherSeverity.Mild,
                WeatherType.Windy => windSpeed.Value >= 55m
                    ? WeatherSeverity.Moderate
                    : WeatherSeverity.Mild,
                WeatherType.Heatwave => WeatherSeverity.Moderate,
                WeatherType.ColdSnap => WeatherSeverity.Moderate,
                WeatherType.Rain => precipitationKind == PrecipitationKind.Rain
                    ? WeatherSeverity.Moderate
                    : WeatherSeverity.Mild,
                WeatherType.Snow => windSpeed.Value >= 25m || precipitationKind == PrecipitationKind.Sleet
                    ? WeatherSeverity.Moderate
                    : WeatherSeverity.Mild,
                WeatherType.Storm => WeatherSeverity.Severe,
                _ => WeatherSeverity.Mild
            };

            return MinSeverity(
                requested: requested,
                allowed: climateProfile.ExtremeWeatherProfile.MaxOverallSeverity);
        }

        private static CloudCoveragePercent DetermineCloudCoverage(
            WeatherType weatherType,
            HumidityPercent humidity,
            PrecipitationKind precipitationKind)
        {
            decimal value = weatherType switch
            {
                WeatherType.Clear => humidity.Value >= 55m ? 28m : 16m,
                WeatherType.Overcast => 78m,
                WeatherType.Rain => precipitationKind == PrecipitationKind.Drizzle ? 82m : 90m,
                WeatherType.Snow => precipitationKind == PrecipitationKind.Sleet ? 88m : 94m,
                WeatherType.Storm => 100m,
                WeatherType.Fog => 70m,
                WeatherType.Windy => 42m,
                WeatherType.Heatwave => 14m,
                WeatherType.ColdSnap => 20m,
                _ => 50m
            };

            return CloudCoveragePercent.From(value);
        }

        private static PressureHpa DeterminePressure(WeatherType weatherType)
        {
            decimal value = weatherType switch
            {
                WeatherType.Clear => 1018m,
                WeatherType.Overcast => 1011m,
                WeatherType.Rain => 1004m,
                WeatherType.Snow => 1007m,
                WeatherType.Storm => 996m,
                WeatherType.Fog => 1013m,
                WeatherType.Windy => 1009m,
                WeatherType.Heatwave => 1017m,
                WeatherType.ColdSnap => 1023m,
                _ => 1012m
            };

            return PressureHpa.From(value);
        }

        private static SimTime ResolveExpectedUntil(
            SimTime startedAt,
            DateTimeOffset localTime)
        {
            int blockStartHour = localTime.Hour / 6 * 6;
            DateTimeOffset localBlockStart = new DateTimeOffset(
                year: localTime.Year,
                month: localTime.Month,
                day: localTime.Day,
                hour: blockStartHour,
                minute: 0,
                second: 0,
                offset: localTime.Offset);

            DateTimeOffset localExpected = localBlockStart.AddHours(6);
            if (localExpected <= localTime)
                localExpected = localExpected.AddHours(6);

            return SimTime.FromUtc(localExpected.ToOffset(TimeSpan.Zero));
        }

        private static WeatherSeason ResolveSeason(
            int month,
            Hemisphere hemisphere)
        {
            WeatherSeason northernSeason = month switch
            {
                3 or 4 or 5 => WeatherSeason.Spring,
                6 or 7 or 8 => WeatherSeason.Summer,
                9 or 10 or 11 => WeatherSeason.Autumn,
                _ => WeatherSeason.Winter
            };

            if (hemisphere == Hemisphere.Northern)
                return northernSeason;

            return northernSeason switch
            {
                WeatherSeason.Spring => WeatherSeason.Autumn,
                WeatherSeason.Summer => WeatherSeason.Winter,
                WeatherSeason.Autumn => WeatherSeason.Spring,
                WeatherSeason.Winter => WeatherSeason.Summer,
                _ => northernSeason
            };
        }

        private static WeatherSeverity MinSeverity(
            WeatherSeverity requested,
            WeatherSeverity allowed)
        {
            return requested <= allowed
                ? requested
                : allowed;
        }
    }
}