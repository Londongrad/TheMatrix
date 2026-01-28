using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather.Enums;
using Matrix.CityCore.Domain.Weather.ValueObjects;

namespace Matrix.CityCore.Domain.Weather
{
    /// <summary>
    ///     Immutable snapshot of the city's current weather conditions.
    /// </summary>
    public sealed record class WeatherState
    {
        private WeatherState() { }

        private WeatherState(
            WeatherType type,
            WeatherSeverity severity,
            PrecipitationKind precipitationKind,
            TemperatureC temperature,
            HumidityPercent humidity,
            WindSpeedKph windSpeed,
            CloudCoveragePercent cloudCoverage,
            PressureHpa pressure,
            SimTime startedAt,
            SimTime expectedUntil)
        {
            Type = type;
            Severity = severity;
            PrecipitationKind = precipitationKind;
            Temperature = temperature;
            Humidity = humidity;
            WindSpeed = windSpeed;
            CloudCoverage = cloudCoverage;
            Pressure = pressure;
            StartedAt = startedAt;
            ExpectedUntil = expectedUntil;
        }

        public WeatherType Type { get; }
        public WeatherSeverity Severity { get; }
        public PrecipitationKind PrecipitationKind { get; }
        public TemperatureC Temperature { get; }
        public HumidityPercent Humidity { get; }
        public WindSpeedKph WindSpeed { get; }
        public CloudCoveragePercent CloudCoverage { get; }
        public PressureHpa Pressure { get; }
        public SimTime StartedAt { get; }
        public SimTime ExpectedUntil { get; }

        public static WeatherState Create(
            WeatherType type,
            WeatherSeverity severity,
            PrecipitationKind precipitationKind,
            TemperatureC temperature,
            HumidityPercent humidity,
            WindSpeedKph windSpeed,
            CloudCoveragePercent cloudCoverage,
            PressureHpa pressure,
            SimTime startedAt,
            SimTime expectedUntil)
        {
            GuardHelper.AgainstInvalidEnum(
                value: type,
                propertyName: nameof(Type));
            GuardHelper.AgainstInvalidEnum(
                value: severity,
                propertyName: nameof(Severity));
            GuardHelper.AgainstInvalidEnum(
                value: precipitationKind,
                propertyName: nameof(PrecipitationKind));

            if (expectedUntil.CompareTo(startedAt) <= 0)
                throw DomainErrorsFactory.InvalidWeatherStateTimeRange(
                    startedAt: startedAt,
                    expectedUntil: expectedUntil,
                    propertyName: nameof(ExpectedUntil));

            EnsurePrecipitationMatchesType(
                type: type,
                precipitationKind: precipitationKind);

            return new WeatherState(
                type: type,
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

        public bool IsActiveAt(SimTime time)
        {
            return time.CompareTo(StartedAt) >= 0 &&
                   time.CompareTo(ExpectedUntil) < 0;
        }

        public bool HasSameConditionsAs(WeatherState other)
        {
            GuardHelper.AgainstNull(
                value: other,
                propertyName: nameof(other));

            return Type == other.Type &&
                   Severity == other.Severity &&
                   PrecipitationKind == other.PrecipitationKind &&
                   Temperature == other.Temperature &&
                   Humidity == other.Humidity &&
                   WindSpeed == other.WindSpeed &&
                   CloudCoverage == other.CloudCoverage &&
                   Pressure == other.Pressure;
        }

        private static void EnsurePrecipitationMatchesType(
            WeatherType type,
            PrecipitationKind precipitationKind)
        {
            bool isValid = type switch
            {
                WeatherType.Clear => precipitationKind == PrecipitationKind.None,
                WeatherType.Overcast => precipitationKind == PrecipitationKind.None,
                WeatherType.Fog => precipitationKind == PrecipitationKind.None,
                WeatherType.Windy => precipitationKind == PrecipitationKind.None,
                WeatherType.Heatwave => precipitationKind == PrecipitationKind.None,
                WeatherType.ColdSnap => precipitationKind == PrecipitationKind.None,
                WeatherType.Rain => precipitationKind == PrecipitationKind.Drizzle ||
                                    precipitationKind == PrecipitationKind.Rain,
                WeatherType.Snow => precipitationKind == PrecipitationKind.Snow ||
                                    precipitationKind == PrecipitationKind.Sleet,
                WeatherType.Storm => precipitationKind != PrecipitationKind.None,
                _ => false
            };

            if (!isValid)
                throw DomainErrorsFactory.IncoherentWeatherPrecipitation(
                    type: type,
                    precipitationKind: precipitationKind,
                    propertyName: nameof(PrecipitationKind));
        }
    }
}
