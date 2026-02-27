using System.Security.Cryptography;
using System.Text;
using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Weather
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
            WeatherState naturalState = planner.PlanNaturalState(
                environment: city.Environment,
                climateProfile: climateProfile,
                evaluatedAt: initialTime);
            WeatherState initialState = city.InitialWeatherProfile.Mode switch
            {
                InitialWeatherMode.Manual => BuildManualInitialState(
                    city: city,
                    templateState: naturalState),
                _ => BuildSeededRandomInitialState(
                    city: city,
                    climateProfile: climateProfile,
                    templateState: naturalState)
            };

            return CityWeather.Create(
                cityId: city.Id,
                climateProfile: climateProfile,
                currentState: initialState,
                createdAt: initialTime);
        }

        private static WeatherState BuildSeededRandomInitialState(
            City city,
            WeatherClimateProfile climateProfile,
            WeatherState templateState)
        {
            decimal typeRoll = GetSeededUnitValue(city, "initial-weather-type");
            decimal detailRoll = GetSeededUnitValue(city, "initial-weather-detail");
            decimal severityRoll = GetSeededUnitValue(city, "initial-weather-severity");
            decimal temperatureRoll = GetSeededCenteredUnitValue(city, "initial-weather-temperature");
            decimal humidityRoll = GetSeededCenteredUnitValue(city, "initial-weather-humidity");
            decimal windRoll = GetSeededCenteredUnitValue(city, "initial-weather-wind");
            decimal cloudRoll = GetSeededCenteredUnitValue(city, "initial-weather-clouds");
            decimal pressureRoll = GetSeededCenteredUnitValue(city, "initial-weather-pressure");

            WeatherType selectedType = DetermineRandomizedType(
                templateState: templateState,
                climateProfile: climateProfile,
                typeRoll: typeRoll,
                detailRoll: detailRoll);
            WeatherSeverity selectedSeverity = DetermineRandomizedSeverity(
                templateState: templateState,
                climateProfile: climateProfile,
                selectedType: selectedType,
                severityRoll: severityRoll);
            decimal temperature = DeriveTemperatureForType(
                type: selectedType,
                severity: selectedSeverity,
                templateState: templateState,
                explicitTemperature: null,
                temperatureRoll: temperatureRoll);

            return ComposeState(
                templateState: templateState,
                selectedType: selectedType,
                selectedSeverity: selectedSeverity,
                temperature: temperature,
                humidityRoll: humidityRoll,
                windRoll: windRoll,
                cloudRoll: cloudRoll,
                pressureRoll: pressureRoll);
        }

        private static WeatherState BuildManualInitialState(
            City city,
            WeatherState templateState)
        {
            WeatherType selectedType = city.InitialWeatherProfile.ManualType ?? WeatherType.Clear;
            WeatherSeverity selectedSeverity = city.InitialWeatherProfile.ManualSeverity ?? WeatherSeverity.Mild;
            decimal temperature = DeriveTemperatureForType(
                type: selectedType,
                severity: selectedSeverity,
                templateState: templateState,
                explicitTemperature: city.InitialWeatherProfile.ManualTemperature?.Value,
                temperatureRoll: 0m);

            return ComposeState(
                templateState: templateState,
                selectedType: selectedType,
                selectedSeverity: selectedSeverity,
                temperature: temperature,
                humidityRoll: 0m,
                windRoll: 0m,
                cloudRoll: 0m,
                pressureRoll: 0m);
        }

        private static WeatherType DetermineRandomizedType(
            WeatherState templateState,
            WeatherClimateProfile climateProfile,
            decimal typeRoll,
            decimal detailRoll)
        {
            bool baselineWet = templateState.Type is WeatherType.Rain or WeatherType.Snow or WeatherType.Storm;

            if (typeRoll < (baselineWet ? 0.18m : 0.30m))
                return SelectDryType(templateState, climateProfile, detailRoll);

            if (typeRoll < (baselineWet ? 0.54m : 0.64m))
                return templateState.Type;

            if (typeRoll < 0.90m)
                return SelectWetType(templateState, climateProfile, detailRoll);

            return SelectExtremeType(templateState, climateProfile, detailRoll);
        }

        private static WeatherType SelectDryType(
            WeatherState templateState,
            WeatherClimateProfile climateProfile,
            decimal detailRoll)
        {
            if (templateState.Temperature.Value >= 24m &&
                climateProfile.ExtremeWeatherProfile.SupportsHeatwaves &&
                detailRoll >= 0.86m)
            {
                return WeatherType.Heatwave;
            }

            if (templateState.Temperature.Value <= -8m && detailRoll >= 0.88m)
                return WeatherType.ColdSnap;

            if (climateProfile.ExtremeWeatherProfile.SupportsFog &&
                templateState.Humidity.Value >= 78m &&
                detailRoll < 0.18m)
            {
                return WeatherType.Fog;
            }

            if (templateState.WindSpeed.Value >= 18m && detailRoll < 0.35m)
                return WeatherType.Windy;

            if (templateState.Humidity.Value >= 62m || detailRoll < 0.72m)
                return WeatherType.Overcast;

            return WeatherType.Clear;
        }

        private static WeatherType SelectWetType(
            WeatherState templateState,
            WeatherClimateProfile climateProfile,
            decimal detailRoll)
        {
            if ((templateState.PrecipitationKind is PrecipitationKind.Snow or PrecipitationKind.Sleet) ||
                templateState.Temperature.Value <= 1m)
            {
                return WeatherType.Snow;
            }

            if (climateProfile.ExtremeWeatherProfile.SupportsThunderstorms && detailRoll >= 0.82m)
                return WeatherType.Storm;

            return WeatherType.Rain;
        }

        private static WeatherType SelectExtremeType(
            WeatherState templateState,
            WeatherClimateProfile climateProfile,
            decimal detailRoll)
        {
            if (templateState.Temperature.Value >= 24m &&
                climateProfile.ExtremeWeatherProfile.SupportsHeatwaves &&
                detailRoll < 0.34m)
            {
                return WeatherType.Heatwave;
            }

            if (templateState.Temperature.Value <= -10m && detailRoll < 0.58m)
                return WeatherType.ColdSnap;

            if (climateProfile.ExtremeWeatherProfile.SupportsThunderstorms &&
                templateState.Temperature.Value >= 0m &&
                detailRoll < 0.84m)
            {
                return WeatherType.Storm;
            }

            if (templateState.WindSpeed.Value >= 16m || detailRoll < 0.92m)
                return WeatherType.Windy;

            return templateState.Type;
        }

        private static WeatherSeverity DetermineRandomizedSeverity(
            WeatherState templateState,
            WeatherClimateProfile climateProfile,
            WeatherType selectedType,
            decimal severityRoll)
        {
            WeatherSeverity severity = selectedType == templateState.Type
                ? templateState.Severity
                : selectedType switch
                {
                    WeatherType.Clear => severityRoll < 0.65m
                        ? WeatherSeverity.Calm
                        : WeatherSeverity.Mild,
                    WeatherType.Overcast => WeatherSeverity.Mild,
                    WeatherType.Fog => WeatherSeverity.Mild,
                    WeatherType.Windy => severityRoll < 0.72m
                        ? WeatherSeverity.Mild
                        : WeatherSeverity.Moderate,
                    WeatherType.Rain => severityRoll < 0.44m
                        ? WeatherSeverity.Mild
                        : WeatherSeverity.Moderate,
                    WeatherType.Snow => severityRoll < 0.52m
                        ? WeatherSeverity.Mild
                        : WeatherSeverity.Moderate,
                    WeatherType.Storm => severityRoll < 0.78m
                        ? WeatherSeverity.Severe
                        : WeatherSeverity.Extreme,
                    WeatherType.Heatwave => severityRoll < 0.70m
                        ? WeatherSeverity.Moderate
                        : WeatherSeverity.Severe,
                    WeatherType.ColdSnap => severityRoll < 0.70m
                        ? WeatherSeverity.Moderate
                        : WeatherSeverity.Severe,
                    _ => WeatherSeverity.Mild
                };

            return severity <= climateProfile.ExtremeWeatherProfile.MaxOverallSeverity
                ? severity
                : climateProfile.ExtremeWeatherProfile.MaxOverallSeverity;
        }

        private static decimal DeriveTemperatureForType(
            WeatherType type,
            WeatherSeverity severity,
            WeatherState templateState,
            decimal? explicitTemperature,
            decimal temperatureRoll)
        {
            if (explicitTemperature.HasValue)
                return Math.Round(explicitTemperature.Value, 2);

            int severityIndex = (int)severity;
            decimal value = type switch
            {
                WeatherType.Clear => templateState.Temperature.Value + 2m + (severityIndex * 0.4m),
                WeatherType.Overcast => templateState.Temperature.Value - 1m,
                WeatherType.Rain => templateState.Temperature.Value - 2m - (severityIndex * 0.5m),
                WeatherType.Snow => Math.Min(templateState.Temperature.Value - 4m - severityIndex, 1m),
                WeatherType.Storm => templateState.Temperature.Value - 3m - severityIndex,
                WeatherType.Fog => templateState.Temperature.Value - 2m,
                WeatherType.Windy => templateState.Temperature.Value - 1m + (severityIndex * 0.2m),
                WeatherType.Heatwave => Math.Max(templateState.Temperature.Value + 8m + (severityIndex * 2m), 30m),
                WeatherType.ColdSnap => Math.Min(templateState.Temperature.Value - 10m - (severityIndex * 2m), -5m),
                _ => templateState.Temperature.Value
            };

            value += temperatureRoll * 3m;
            value = Math.Clamp(value, TemperatureC.Min, TemperatureC.Max);
            return Math.Round(value, 2);
        }

        private static WeatherState ComposeState(
            WeatherState templateState,
            WeatherType selectedType,
            WeatherSeverity selectedSeverity,
            decimal temperature,
            decimal humidityRoll,
            decimal windRoll,
            decimal cloudRoll,
            decimal pressureRoll)
        {
            int severityIndex = (int)selectedSeverity;
            PrecipitationKind precipitationKind = ResolvePrecipitationKind(
                type: selectedType,
                severity: selectedSeverity,
                temperature: temperature);

            decimal humidity = ResolveHumidity(
                templateState: templateState,
                selectedType: selectedType,
                severityIndex: severityIndex,
                humidityRoll: humidityRoll);
            decimal windSpeed = ResolveWindSpeed(
                templateState: templateState,
                selectedType: selectedType,
                severityIndex: severityIndex,
                windRoll: windRoll);
            decimal cloudCoverage = ResolveCloudCoverage(
                selectedType: selectedType,
                severityIndex: severityIndex,
                cloudRoll: cloudRoll);
            decimal pressure = ResolvePressure(
                selectedType: selectedType,
                severityIndex: severityIndex,
                pressureRoll: pressureRoll);

            return WeatherState.Create(
                type: selectedType,
                severity: selectedSeverity,
                precipitationKind: precipitationKind,
                temperature: TemperatureC.From(temperature),
                humidity: HumidityPercent.From(humidity),
                windSpeed: WindSpeedKph.From(windSpeed),
                cloudCoverage: CloudCoveragePercent.From(cloudCoverage),
                pressure: PressureHpa.From(pressure),
                startedAt: templateState.StartedAt,
                expectedUntil: templateState.ExpectedUntil);
        }

        private static PrecipitationKind ResolvePrecipitationKind(
            WeatherType type,
            WeatherSeverity severity,
            decimal temperature)
        {
            return type switch
            {
                WeatherType.Clear => PrecipitationKind.None,
                WeatherType.Overcast => PrecipitationKind.None,
                WeatherType.Fog => PrecipitationKind.None,
                WeatherType.Windy => PrecipitationKind.None,
                WeatherType.Heatwave => PrecipitationKind.None,
                WeatherType.ColdSnap => PrecipitationKind.None,
                WeatherType.Rain => severity <= WeatherSeverity.Mild
                    ? PrecipitationKind.Drizzle
                    : PrecipitationKind.Rain,
                WeatherType.Snow => temperature > 0m || severity <= WeatherSeverity.Mild
                    ? PrecipitationKind.Sleet
                    : PrecipitationKind.Snow,
                WeatherType.Storm => temperature <= 0m
                    ? PrecipitationKind.Hail
                    : PrecipitationKind.Rain,
                _ => PrecipitationKind.None
            };
        }

        private static decimal ResolveHumidity(
            WeatherState templateState,
            WeatherType selectedType,
            int severityIndex,
            decimal humidityRoll)
        {
            decimal value = selectedType switch
            {
                WeatherType.Clear => Math.Min(templateState.Humidity.Value - 18m + (humidityRoll * 10m), 58m),
                WeatherType.Overcast => Math.Max(templateState.Humidity.Value + 8m + (humidityRoll * 8m), 58m),
                WeatherType.Rain => Math.Max(templateState.Humidity.Value + 18m + (severityIndex * 2m) + (humidityRoll * 6m), 72m),
                WeatherType.Snow => Math.Max(templateState.Humidity.Value + 12m + (severityIndex * 2m) + (humidityRoll * 7m), 65m),
                WeatherType.Storm => Math.Max(templateState.Humidity.Value + 22m + (severityIndex * 2m) + (humidityRoll * 5m), 78m),
                WeatherType.Fog => Math.Max(templateState.Humidity.Value + 15m + (humidityRoll * 5m), 85m),
                WeatherType.Windy => templateState.Humidity.Value - 8m + (humidityRoll * 12m),
                WeatherType.Heatwave => Math.Min(templateState.Humidity.Value - 24m + (humidityRoll * 8m), 45m),
                WeatherType.ColdSnap => templateState.Humidity.Value + 4m + (humidityRoll * 10m),
                _ => templateState.Humidity.Value
            };

            value = Math.Clamp(value, HumidityPercent.Min, HumidityPercent.Max);
            return Math.Round(value, 2);
        }

        private static decimal ResolveWindSpeed(
            WeatherState templateState,
            WeatherType selectedType,
            int severityIndex,
            decimal windRoll)
        {
            decimal value = selectedType switch
            {
                WeatherType.Clear => templateState.WindSpeed.Value + (severityIndex * 1.5m) + (windRoll * 4m),
                WeatherType.Overcast => templateState.WindSpeed.Value + 2m + (windRoll * 4m),
                WeatherType.Rain => templateState.WindSpeed.Value + 6m + (severityIndex * 3m) + (windRoll * 5m),
                WeatherType.Snow => templateState.WindSpeed.Value + 5m + (severityIndex * 3m) + (windRoll * 5m),
                WeatherType.Storm => Math.Max(templateState.WindSpeed.Value + 20m + (severityIndex * 7m) + (windRoll * 6m), 35m),
                WeatherType.Fog => Math.Max(templateState.WindSpeed.Value - 5m + (windRoll * 3m), 0m),
                WeatherType.Windy => Math.Max(templateState.WindSpeed.Value + 16m + (severityIndex * 5m) + (windRoll * 6m), 22m),
                WeatherType.Heatwave => Math.Max(templateState.WindSpeed.Value - 2m + (windRoll * 4m), 0m),
                WeatherType.ColdSnap => templateState.WindSpeed.Value + 8m + (severityIndex * 4m) + (windRoll * 5m),
                _ => templateState.WindSpeed.Value
            };

            value = Math.Clamp(value, WindSpeedKph.Min, WindSpeedKph.Max);
            return Math.Round(value, 2);
        }

        private static decimal ResolveCloudCoverage(
            WeatherType selectedType,
            int severityIndex,
            decimal cloudRoll)
        {
            decimal value = selectedType switch
            {
                WeatherType.Clear => 12m + (severityIndex * 4m) + (cloudRoll * 8m),
                WeatherType.Overcast => 78m + (severityIndex * 3m) + (cloudRoll * 6m),
                WeatherType.Rain => 84m + (severityIndex * 3m) + (cloudRoll * 6m),
                WeatherType.Snow => 88m + (severityIndex * 3m) + (cloudRoll * 5m),
                WeatherType.Storm => 96m + (severityIndex * 1m) + (cloudRoll * 3m),
                WeatherType.Fog => 68m + (severityIndex * 2m) + (cloudRoll * 8m),
                WeatherType.Windy => 44m + (severityIndex * 3m) + (cloudRoll * 12m),
                WeatherType.Heatwave => 8m + (severityIndex * 2m) + (cloudRoll * 6m),
                WeatherType.ColdSnap => 20m + (severityIndex * 4m) + (cloudRoll * 10m),
                _ => 50m
            };

            value = Math.Clamp(value, CloudCoveragePercent.Min, CloudCoveragePercent.Max);
            return Math.Round(value, 2);
        }

        private static decimal ResolvePressure(
            WeatherType selectedType,
            int severityIndex,
            decimal pressureRoll)
        {
            decimal value = selectedType switch
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

            value += selectedType switch
            {
                WeatherType.Storm => -(severityIndex * 2m),
                WeatherType.Rain => -(severityIndex * 1m),
                WeatherType.Snow => -(severityIndex * 1m),
                WeatherType.Clear => severityIndex * 0.5m,
                WeatherType.ColdSnap => severityIndex * 1m,
                _ => 0m
            };

            value += pressureRoll * 4m;
            value = Math.Clamp(value, PressureHpa.Min, PressureHpa.Max);
            return Math.Round(value, 2);
        }

        private static decimal GetSeededUnitValue(
            City city,
            string salt)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(BuildSeedInput(city, salt)));
            uint sample = BitConverter.ToUInt32(hash, 0);
            return sample / (decimal)uint.MaxValue;
        }

        private static decimal GetSeededCenteredUnitValue(
            City city,
            string salt)
        {
            return (GetSeededUnitValue(city, salt) - 0.5m) * 2m;
        }

        private static string BuildSeedInput(
            City city,
            string salt)
        {
            return $"{city.GenerationSeed.Value}|{city.Environment.ClimateZone}|{city.Environment.Hemisphere}|{salt}";
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
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(climateZone),
                    actualValue: climateZone,
                    message: null)
            };
        }
    }
}
