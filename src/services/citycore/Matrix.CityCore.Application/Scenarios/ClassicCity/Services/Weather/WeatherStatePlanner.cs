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
            SimTime evaluatedAt)
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
            TemperatureC temperature = CalculateTemperature(
                climateProfile: climateProfile,
                season: season,
                representativeHour: representativeHour,
                volatilityFactor: volatilityFactor);

            HumidityPercent humidity = CalculateHumidity(
                climateProfile: climateProfile,
                season: season,
                representativeHour: representativeHour,
                volatilityFactor: volatilityFactor);

            WindSpeedKph windSpeed = CalculateWindSpeed(
                climateProfile: climateProfile,
                season: season,
                representativeHour: representativeHour,
                volatilityFactor: volatilityFactor);

            PrecipitationKind precipitationKind = DeterminePrecipitationKind(
                climateProfile: climateProfile,
                season: season,
                temperature: temperature,
                representativeHour: representativeHour,
                volatilityFactor: volatilityFactor);

            WeatherType weatherType = DetermineWeatherType(
                climateProfile: climateProfile,
                temperature: temperature,
                humidity: humidity,
                windSpeed: windSpeed,
                precipitationKind: precipitationKind,
                representativeHour: representativeHour);

            WeatherSeverity severity = DetermineSeverity(
                climateProfile: climateProfile,
                weatherType: weatherType,
                precipitationKind: precipitationKind,
                windSpeed: windSpeed,
                volatilityFactor: volatilityFactor);

            CloudCoveragePercent cloudCoverage = DetermineCloudCoverage(
                weatherType: weatherType,
                humidity: humidity,
                precipitationKind: precipitationKind,
                volatilityFactor: volatilityFactor);

            PressureHpa pressure = DeterminePressure(
                weatherType: weatherType,
                volatilityFactor: volatilityFactor);

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
            decimal volatilityFactor)
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
            return TemperatureC.From(
                Math.Round(
                    d: value,
                    decimals: 2));
        }

        private static HumidityPercent CalculateHumidity(
            WeatherClimateProfile climateProfile,
            WeatherSeason season,
            int representativeHour,
            decimal volatilityFactor)
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
            decimal volatilityFactor)
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
            int representativeHour,
            decimal volatilityFactor)
        {
            PrecipitationKind dominantKind = climateProfile.GetDominantPrecipitation(season);

            if (dominantKind == PrecipitationKind.None && volatilityFactor >= 1.25m && representativeHour >= 12)
                return PrecipitationKind.Drizzle;

            return dominantKind switch
            {
                PrecipitationKind.None => PrecipitationKind.None,
                PrecipitationKind.Drizzle => volatilityFactor >= 1.30m && representativeHour >= 12
                    ? PrecipitationKind.Rain
                    : PrecipitationKind.Drizzle,
                PrecipitationKind.Rain => representativeHour < 6
                    ? PrecipitationKind.Drizzle
                    : PrecipitationKind.Rain,
                PrecipitationKind.Snow => temperature.Value > 0m
                    ? PrecipitationKind.Sleet
                    : PrecipitationKind.Snow,
                PrecipitationKind.Sleet => temperature.Value <= 0m
                    ? PrecipitationKind.Snow
                    : PrecipitationKind.Sleet,
                PrecipitationKind.Hail => volatilityFactor >= 1.15m
                    ? PrecipitationKind.Hail
                    : PrecipitationKind.Rain,
                _ => PrecipitationKind.None
            };
        }

        private static WeatherType DetermineWeatherType(
            WeatherClimateProfile climateProfile,
            TemperatureC temperature,
            HumidityPercent humidity,
            WindSpeedKph windSpeed,
            PrecipitationKind precipitationKind,
            int representativeHour)
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
                (representativeHour < 6 || representativeHour >= 18))
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
            WindSpeedKph windSpeed,
            decimal volatilityFactor)
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

            if (volatilityFactor >= 1.28m && requested < WeatherSeverity.Severe)
                requested++;

            return requested <= climateProfile.ExtremeWeatherProfile.MaxOverallSeverity
                ? requested
                : climateProfile.ExtremeWeatherProfile.MaxOverallSeverity;
        }

        private static CloudCoveragePercent DetermineCloudCoverage(
            WeatherType weatherType,
            HumidityPercent humidity,
            PrecipitationKind precipitationKind,
            decimal volatilityFactor)
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
            decimal volatilityFactor)
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
            value = Math.Clamp(
                value: value,
                min: PressureHpa.Min,
                max: PressureHpa.Max);
            return PressureHpa.From(
                Math.Round(
                    d: value,
                    decimals: 2));
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
    }
}
