using System.Security.Cryptography;
using System.Text;
using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Weather
{
    /// <summary>
    ///     Builds deterministic natural weather states from city context and climate profile.
    /// </summary>
    public sealed class WeatherStatePlanner : IWeatherStatePlanner
    {
        private const int WeatherBlockHours = 6;

        public WeatherState PlanNaturalState(
            CityEnvironment environment,
            WeatherClimateProfile climateProfile,
            CityGenerationSeed generationSeed,
            SimTime evaluatedAt,
            WeatherState? previousState = null)
        {
            ArgumentNullException.ThrowIfNull(environment);
            ArgumentNullException.ThrowIfNull(climateProfile);

            DateTimeOffset localTime = evaluatedAt.ValueUtc.ToOffset(environment.UtcOffset.Value);
            DateTimeOffset localWindowStart = ResolveLocalWindowStart(localTime);
            DateTimeOffset localWindowEnd = localWindowStart.AddHours(WeatherBlockHours);
            int representativeHour = (localWindowStart.Hour + (WeatherBlockHours / 2)) % 24;

            WeatherSeason season = ResolveSeason(
                month: localWindowStart.Month,
                hemisphere: environment.Hemisphere);

            decimal volatilityFactor = ResolveVolatilityFactor(climateProfile.Volatility);
            decimal temperatureRoll = GetSeededCenteredUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "temperature");
            decimal humidityRoll = GetSeededCenteredUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "humidity");
            decimal windRoll = GetSeededCenteredUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "wind");
            decimal precipitationRoll = GetSeededUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "precipitation");
            decimal precipitationKindRoll = GetSeededUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "precipitation-kind");
            decimal weatherTypeRoll = GetSeededUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "weather-type");
            decimal severityRoll = GetSeededUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "severity");
            decimal cloudRoll = GetSeededCenteredUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "clouds");
            decimal pressureRoll = GetSeededCenteredUnitValue(
                generationSeed: generationSeed,
                environment: environment,
                localWindowStart: localWindowStart,
                salt: "pressure");

            TemperatureC temperature = CalculateTemperature(
                climateProfile: climateProfile,
                season: season,
                representativeHour: representativeHour,
                volatilityFactor: volatilityFactor,
                temperatureRoll: temperatureRoll);

            HumidityPercent humidity = CalculateHumidity(
                climateProfile: climateProfile,
                season: season,
                representativeHour: representativeHour,
                volatilityFactor: volatilityFactor,
                humidityRoll: humidityRoll);

            WindSpeedKph windSpeed = CalculateWindSpeed(
                climateProfile: climateProfile,
                season: season,
                representativeHour: representativeHour,
                volatilityFactor: volatilityFactor,
                windRoll: windRoll);

            PrecipitationKind precipitationKind = DeterminePrecipitationKind(
                climateProfile: climateProfile,
                season: season,
                temperature: temperature,
                humidity: humidity,
                windSpeed: windSpeed,
                representativeHour: representativeHour,
                previousState: previousState,
                localWindowStart: localWindowStart,
                precipitationRoll: precipitationRoll,
                precipitationKindRoll: precipitationKindRoll);

            WeatherType weatherType = DetermineWeatherType(
                climateProfile: climateProfile,
                temperature: temperature,
                humidity: humidity,
                windSpeed: windSpeed,
                precipitationKind: precipitationKind,
                representativeHour: representativeHour,
                weatherTypeRoll: weatherTypeRoll);

            WeatherSeverity severity = DetermineSeverity(
                climateProfile: climateProfile,
                weatherType: weatherType,
                precipitationKind: precipitationKind,
                windSpeed: windSpeed,
                volatilityFactor: volatilityFactor,
                severityRoll: severityRoll);

            CloudCoveragePercent cloudCoverage = DetermineCloudCoverage(
                weatherType: weatherType,
                humidity: humidity,
                precipitationKind: precipitationKind,
                volatilityFactor: volatilityFactor,
                cloudRoll: cloudRoll);

            PressureHpa pressure = DeterminePressure(
                weatherType: weatherType,
                volatilityFactor: volatilityFactor,
                pressureRoll: pressureRoll);

            return WeatherState.Create(
                type: weatherType,
                severity: severity,
                precipitationKind: precipitationKind,
                temperature: temperature,
                humidity: humidity,
                windSpeed: windSpeed,
                cloudCoverage: cloudCoverage,
                pressure: pressure,
                startedAt: SimTime.FromUtc(localWindowStart.ToOffset(TimeSpan.Zero)),
                expectedUntil: SimTime.FromUtc(localWindowEnd.ToOffset(TimeSpan.Zero)));
        }

        private static DateTimeOffset ResolveLocalWindowStart(DateTimeOffset localTime)
        {
            int blockStartHour = localTime.Hour / WeatherBlockHours * WeatherBlockHours;

            return new DateTimeOffset(
                year: localTime.Year,
                month: localTime.Month,
                day: localTime.Day,
                hour: blockStartHour,
                minute: 0,
                second: 0,
                offset: localTime.Offset);
        }

        private static decimal ResolveVolatilityFactor(WeatherVolatility volatility)
        {
            return 0.70m + (volatility.Value * 0.80m);
        }

        private static TemperatureC CalculateTemperature(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            int representativeHour,
            decimal volatilityFactor,
            decimal temperatureRoll)
        {
            decimal baseline = climateProfile.GetBaselineTemperature(season)
               .Value;
            decimal swing = climateProfile.TemperatureProfile.DailySwing.Value;
            decimal multiplier = representativeHour switch
            {
                >= 0 and < 6 => -0.45m,
                >= 6 and < 12 => 0.10m,
                >= 12 and < 18 => 0.45m,
                _ => -0.10m
            };

            decimal value = baseline + (swing * multiplier * volatilityFactor);
            value += temperatureRoll * (1.10m + (volatilityFactor * 1.80m));
            return TemperatureC.From(
                Math.Round(
                    d: value,
                    decimals: 2));
        }

        private static HumidityPercent CalculateHumidity(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            int representativeHour,
            decimal volatilityFactor,
            decimal humidityRoll)
        {
            decimal baseline = climateProfile.GetBaselineHumidity(season)
               .Value;
            decimal adjustment = representativeHour switch
            {
                >= 0 and < 6 => 7m,
                >= 6 and < 12 => 3m,
                >= 12 and < 18 => -6m,
                _ => 1m
            };

            decimal value = baseline + (adjustment * volatilityFactor);
            value += humidityRoll * (4m + (volatilityFactor * 4m));
            value = Math.Clamp(
                value: value,
                min: HumidityPercent.Min,
                max: HumidityPercent.Max);
            return HumidityPercent.From(
                Math.Round(
                    d: value,
                    decimals: 2));
        }

        private static WindSpeedKph CalculateWindSpeed(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            int representativeHour,
            decimal volatilityFactor,
            decimal windRoll)
        {
            decimal baseline = climateProfile.GetBaselineWindSpeed(season)
               .Value;
            decimal adjustment = representativeHour switch
            {
                >= 0 and < 6 => -2m,
                >= 6 and < 12 => 2m,
                >= 12 and < 18 => 5m,
                _ => 1m
            };

            decimal value = baseline + (adjustment * volatilityFactor);
            value += windRoll * (2.5m + (volatilityFactor * 4.5m));
            value = Math.Clamp(
                value: value,
                min: WindSpeedKph.Min,
                max: WindSpeedKph.Max);
            return WindSpeedKph.From(
                Math.Round(
                    d: value,
                    decimals: 2));
        }

        private static PrecipitationKind DeterminePrecipitationKind(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            TemperatureC temperature,
            HumidityPercent humidity,
            WindSpeedKph windSpeed,
            int representativeHour,
            WeatherState? previousState,
            DateTimeOffset localWindowStart,
            decimal precipitationRoll,
            decimal precipitationKindRoll)
        {
            PrecipitationKind dominantKind = climateProfile.GetDominantPrecipitation(season);
            decimal wetProbability = ResolveWetProbability(
                climateProfile: climateProfile,
                dominantKind: dominantKind,
                humidity: humidity,
                representativeHour: representativeHour,
                previousState: previousState,
                localWindowStart: localWindowStart);

            if (precipitationRoll > wetProbability)
                return PrecipitationKind.None;

            if (temperature.Value <= -2m)
            {
                return precipitationKindRoll >= 0.78m &&
                       dominantKind == PrecipitationKind.Sleet
                    ? PrecipitationKind.Sleet
                    : PrecipitationKind.Snow;
            }

            if (temperature.Value < 2m)
            {
                return dominantKind is PrecipitationKind.Snow or PrecipitationKind.Sleet ||
                       precipitationKindRoll < 0.58m
                    ? PrecipitationKind.Sleet
                    : PrecipitationKind.Drizzle;
            }

            if (climateProfile.ExtremeWeatherProfile.SupportsThunderstorms &&
                humidity.Value >= 82m &&
                windSpeed.Value >= 28m &&
                precipitationKindRoll >= 0.96m)
            {
                return PrecipitationKind.Hail;
            }

            if (dominantKind == PrecipitationKind.Drizzle && precipitationKindRoll < 0.64m)
                return PrecipitationKind.Drizzle;

            if (dominantKind == PrecipitationKind.None)
                return precipitationKindRoll < 0.55m
                    ? PrecipitationKind.Drizzle
                    : PrecipitationKind.Rain;

            if (precipitationKindRoll < 0.30m && representativeHour < 6)
                return PrecipitationKind.Drizzle;

            return PrecipitationKind.Rain;
        }

        private static WeatherType DetermineWeatherType(
            WeatherClimateProfile climateProfile,
            TemperatureC temperature,
            HumidityPercent humidity,
            WindSpeedKph windSpeed,
            PrecipitationKind precipitationKind,
            int representativeHour,
            decimal weatherTypeRoll)
        {
            if (precipitationKind == PrecipitationKind.Hail)
                return WeatherType.Storm;

            if ((precipitationKind == PrecipitationKind.Rain || precipitationKind == PrecipitationKind.Drizzle) &&
                climateProfile.ExtremeWeatherProfile.SupportsThunderstorms &&
                humidity.Value >= 82m &&
                windSpeed.Value >= 26m &&
                weatherTypeRoll >= 0.84m)
            {
                return WeatherType.Storm;
            }

            if ((precipitationKind == PrecipitationKind.Snow || precipitationKind == PrecipitationKind.Sleet) &&
                climateProfile.ExtremeWeatherProfile.SupportsSnowstorms &&
                windSpeed.Value >= 30m &&
                weatherTypeRoll >= 0.80m)
            {
                return WeatherType.Storm;
            }

            if (precipitationKind == PrecipitationKind.Rain || precipitationKind == PrecipitationKind.Drizzle)
                return WeatherType.Rain;

            if (precipitationKind == PrecipitationKind.Snow || precipitationKind == PrecipitationKind.Sleet)
                return WeatherType.Snow;

            if (climateProfile.ExtremeWeatherProfile.SupportsHeatwaves &&
                temperature.Value >= 33m &&
                humidity.Value <= 58m &&
                weatherTypeRoll >= 0.52m)
            {
                return WeatherType.Heatwave;
            }

            if (temperature.Value <= -18m && weatherTypeRoll >= 0.40m)
                return WeatherType.ColdSnap;

            if (climateProfile.ExtremeWeatherProfile.SupportsFog &&
                humidity.Value >= 90m &&
                windSpeed.Value <= 15m &&
                (representativeHour < 6 || representativeHour >= 18) &&
                weatherTypeRoll <= 0.42m)
            {
                return WeatherType.Fog;
            }

            if (windSpeed.Value >= 36m || (windSpeed.Value >= 28m && weatherTypeRoll >= 0.86m))
                return WeatherType.Windy;

            if (humidity.Value >= 68m || (humidity.Value >= 60m && weatherTypeRoll >= 0.76m))
                return WeatherType.Overcast;

            return WeatherType.Clear;
        }

        private static WeatherSeverity DetermineSeverity(
            WeatherClimateProfile climateProfile,
            WeatherType weatherType,
            PrecipitationKind precipitationKind,
            WindSpeedKph windSpeed,
            decimal volatilityFactor,
            decimal severityRoll)
        {
            int requestedValue = weatherType switch
            {
                WeatherType.Clear => windSpeed.Value >= 20m
                    ? (int)WeatherSeverity.Mild
                    : (int)WeatherSeverity.Calm,
                WeatherType.Overcast => (int)WeatherSeverity.Mild,
                WeatherType.Fog => (int)WeatherSeverity.Mild,
                WeatherType.Windy => windSpeed.Value >= 55m
                    ? (int)WeatherSeverity.Moderate
                    : (int)WeatherSeverity.Mild,
                WeatherType.Heatwave => (int)WeatherSeverity.Moderate,
                WeatherType.ColdSnap => (int)WeatherSeverity.Moderate,
                WeatherType.Rain => precipitationKind == PrecipitationKind.Rain
                    ? (int)WeatherSeverity.Moderate
                    : (int)WeatherSeverity.Mild,
                WeatherType.Snow => windSpeed.Value >= 25m || precipitationKind == PrecipitationKind.Sleet
                    ? (int)WeatherSeverity.Moderate
                    : (int)WeatherSeverity.Mild,
                WeatherType.Storm => (int)WeatherSeverity.Severe,
                _ => (int)WeatherSeverity.Mild
            };

            if (severityRoll >= 0.78m && requestedValue < (int)WeatherSeverity.Severe)
                requestedValue++;

            if (weatherType is WeatherType.Storm or WeatherType.Heatwave or WeatherType.ColdSnap &&
                severityRoll >= 0.94m &&
                requestedValue < (int)WeatherSeverity.Extreme)
            {
                requestedValue++;
            }

            if (severityRoll <= 0.14m &&
                weatherType != WeatherType.Storm &&
                requestedValue > (int)WeatherSeverity.Calm)
            {
                requestedValue--;
            }

            if (volatilityFactor >= 1.35m &&
                severityRoll >= 0.60m &&
                requestedValue < (int)WeatherSeverity.Severe)
            {
                requestedValue++;
            }

            requestedValue = Math.Clamp(
                value: requestedValue,
                min: (int)WeatherSeverity.Calm,
                max: (int)WeatherSeverity.Extreme);
            WeatherSeverity requested = (WeatherSeverity)requestedValue;

            return requested <= climateProfile.ExtremeWeatherProfile.MaxOverallSeverity
                ? requested
                : climateProfile.ExtremeWeatherProfile.MaxOverallSeverity;
        }

        private static CloudCoveragePercent DetermineCloudCoverage(
            WeatherType weatherType,
            HumidityPercent humidity,
            PrecipitationKind precipitationKind,
            decimal volatilityFactor,
            decimal cloudRoll)
        {
            decimal value = weatherType switch
            {
                WeatherType.Clear => humidity.Value >= 55m
                    ? 28m
                    : 16m,
                WeatherType.Overcast => 78m,
                WeatherType.Rain => precipitationKind == PrecipitationKind.Drizzle
                    ? 82m
                    : 90m,
                WeatherType.Snow => precipitationKind == PrecipitationKind.Sleet
                    ? 88m
                    : 94m,
                WeatherType.Storm => 100m,
                WeatherType.Fog => 70m,
                WeatherType.Windy => 42m,
                WeatherType.Heatwave => 14m,
                WeatherType.ColdSnap => 20m,
                _ => 50m
            };

            if (weatherType == WeatherType.Clear || weatherType == WeatherType.Windy)
                value += (volatilityFactor - 1m) * 8m;

            value += cloudRoll * (weatherType switch
            {
                WeatherType.Clear => 10m,
                WeatherType.Windy => 12m,
                WeatherType.Heatwave => 8m,
                WeatherType.Fog => 6m,
                _ => 5m
            });
            value = Math.Clamp(
                value: value,
                min: CloudCoveragePercent.Min,
                max: CloudCoveragePercent.Max);
            return CloudCoveragePercent.From(
                Math.Round(
                    d: value,
                    decimals: 2));
        }

        private static PressureHpa DeterminePressure(
            WeatherType weatherType,
            decimal volatilityFactor,
            decimal pressureRoll)
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

            decimal adjustment = weatherType switch
            {
                WeatherType.Storm => -3m,
                WeatherType.Windy => -1m,
                WeatherType.Clear => 1m,
                _ => 0m
            };

            value += adjustment * (volatilityFactor - 1m);
            value += pressureRoll * 5m;
            value = Math.Clamp(
                value: value,
                min: PressureHpa.Min,
                max: PressureHpa.Max);
            return PressureHpa.From(
                Math.Round(
                    d: value,
                    decimals: 2));
        }

        private static decimal ResolveWetProbability(
            WeatherClimateProfile climateProfile,
            PrecipitationKind dominantKind,
            HumidityPercent humidity,
            int representativeHour,
            WeatherState? previousState,
            DateTimeOffset localWindowStart)
        {
            decimal humidityFactor = humidity.Value / HumidityPercent.Max;
            decimal dominantBias = dominantKind switch
            {
                PrecipitationKind.None => 0.06m + (humidityFactor * 0.16m),
                PrecipitationKind.Drizzle => 0.22m + (humidityFactor * 0.18m),
                PrecipitationKind.Rain => 0.34m + (humidityFactor * 0.24m),
                PrecipitationKind.Snow => 0.36m + (humidityFactor * 0.18m),
                PrecipitationKind.Sleet => 0.30m + (humidityFactor * 0.20m),
                PrecipitationKind.Hail => 0.28m + (humidityFactor * 0.20m),
                _ => 0.12m
            };

            decimal timeOfDayBias = representativeHour switch
            {
                >= 0 and < 6 => -0.10m,
                >= 6 and < 12 => -0.02m,
                >= 12 and < 18 => 0.08m,
                _ => 0.03m
            };

            decimal continuityBias = ResolveWetContinuityBias(
                previousState: previousState,
                localWindowStart: localWindowStart);

            decimal probability = dominantBias +
                                  timeOfDayBias +
                                  (climateProfile.Volatility.Value * 0.22m) +
                                  continuityBias;

            return Math.Clamp(
                value: probability,
                min: 0.05m,
                max: 0.92m);
        }

        private static decimal ResolveWetContinuityBias(
            WeatherState? previousState,
            DateTimeOffset localWindowStart)
        {
            if (previousState is null)
                return 0m;

            bool previousWet = previousState.PrecipitationKind != PrecipitationKind.None;
            int streakBlocks = CalculateStreakBlocks(
                previousState: previousState,
                localWindowStart: localWindowStart);

            if (previousWet)
            {
                decimal persistencePenalty = Math.Min(
                    val1: 0.18m,
                    val2: Math.Max(
                        val1: 0,
                        val2: streakBlocks - 1) * 0.05m);
                return 0.12m - persistencePenalty;
            }

            decimal recoveryBoost = Math.Min(
                val1: 0.12m,
                val2: Math.Max(
                    val1: 0,
                    val2: streakBlocks - 2) * 0.03m);
            return -0.06m + recoveryBoost;
        }

        private static int CalculateStreakBlocks(
            WeatherState previousState,
            DateTimeOffset localWindowStart)
        {
            DateTimeOffset currentWindowStartUtc = localWindowStart.ToUniversalTime();

            if (currentWindowStartUtc <= previousState.StartedAt.ValueUtc)
                return 1;

            double elapsedBlocks = (currentWindowStartUtc - previousState.StartedAt.ValueUtc)
               .TotalHours / WeatherBlockHours;
            return Math.Max(
                val1: 1,
                val2: (int)Math.Floor(elapsedBlocks));
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

        private static decimal GetSeededUnitValue(
            CityGenerationSeed generationSeed,
            CityEnvironment environment,
            DateTimeOffset localWindowStart,
            string salt)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    BuildSeedInput(
                        generationSeed: generationSeed,
                        environment: environment,
                        localWindowStart: localWindowStart,
                        salt: salt)));
            uint sample = BitConverter.ToUInt32(hash, 0);
            return sample / (decimal)uint.MaxValue;
        }

        private static decimal GetSeededCenteredUnitValue(
            CityGenerationSeed generationSeed,
            CityEnvironment environment,
            DateTimeOffset localWindowStart,
            string salt)
        {
            return (GetSeededUnitValue(
                        generationSeed: generationSeed,
                        environment: environment,
                        localWindowStart: localWindowStart,
                        salt: salt) - 0.5m) * 2m;
        }

        private static string BuildSeedInput(
            CityGenerationSeed generationSeed,
            CityEnvironment environment,
            DateTimeOffset localWindowStart,
            string salt)
        {
            return FormattableString.Invariant(
                $"weather|{generationSeed.Value}|{environment.ClimateZone}|{environment.Hemisphere}|{localWindowStart:yyyy-MM-dd-HH}|{salt}");
        }
    }
}
