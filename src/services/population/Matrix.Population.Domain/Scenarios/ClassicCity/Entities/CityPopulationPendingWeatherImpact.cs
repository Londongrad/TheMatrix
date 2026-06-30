using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationPendingWeatherImpact
    {
        private CityPopulationPendingWeatherImpact() { }

        private CityPopulationPendingWeatherImpact(
            Guid impactId,
            CityId cityId,
            DateOnly currentDate,
            WeatherImpactProfile previousWeather,
            WeatherImpactProfile currentWeather,
            CityPopulationEnvironment? environment,
            DateTimeOffset occurredAtUtc)
        {
            EnsureUtc(
                value: occurredAtUtc,
                paramName: nameof(occurredAtUtc));

            ImpactId = GuardHelper.AgainstEmptyGuid(
                id: impactId,
                propertyName: nameof(ImpactId));
            CityId = cityId;
            CurrentDate = currentDate;
            SetPreviousWeather(GuardHelper.AgainstNull(previousWeather, nameof(previousWeather)));
            SetCurrentWeather(GuardHelper.AgainstNull(currentWeather, nameof(currentWeather)));
            EnvironmentClimateZone = environment?.ClimateZone;
            EnvironmentHemisphere = environment?.Hemisphere;
            EnvironmentUtcOffsetMinutes = environment?.UtcOffsetMinutes;
            OccurredAtUtc = occurredAtUtc;
        }

        public Guid ImpactId { get; private set; }
        public CityId CityId { get; private set; }
        public DateOnly CurrentDate { get; private set; }

        public PopulationWeatherType PreviousType { get; private set; }
        public PopulationWeatherSeverity PreviousSeverity { get; private set; }
        public PopulationPrecipitationKind PreviousPrecipitationKind { get; private set; }
        public decimal PreviousTemperatureC { get; private set; }
        public decimal PreviousHumidityPercent { get; private set; }
        public decimal PreviousWindSpeedKph { get; private set; }
        public decimal PreviousCloudCoveragePercent { get; private set; }
        public decimal PreviousPressureHpa { get; private set; }

        public PopulationWeatherType CurrentType { get; private set; }
        public PopulationWeatherSeverity CurrentSeverity { get; private set; }
        public PopulationPrecipitationKind CurrentPrecipitationKind { get; private set; }
        public decimal CurrentTemperatureC { get; private set; }
        public decimal CurrentHumidityPercent { get; private set; }
        public decimal CurrentWindSpeedKph { get; private set; }
        public decimal CurrentCloudCoveragePercent { get; private set; }
        public decimal CurrentPressureHpa { get; private set; }

        public PopulationClimateZone? EnvironmentClimateZone { get; private set; }
        public PopulationHemisphere? EnvironmentHemisphere { get; private set; }
        public int? EnvironmentUtcOffsetMinutes { get; private set; }
        public DateTimeOffset OccurredAtUtc { get; private set; }

        public WeatherImpactProfile PreviousWeather => new(
            Type: PreviousType,
            Severity: PreviousSeverity,
            PrecipitationKind: PreviousPrecipitationKind,
            TemperatureC: PreviousTemperatureC,
            HumidityPercent: PreviousHumidityPercent,
            WindSpeedKph: PreviousWindSpeedKph,
            CloudCoveragePercent: PreviousCloudCoveragePercent,
            PressureHpa: PreviousPressureHpa);

        public WeatherImpactProfile CurrentWeather => new(
            Type: CurrentType,
            Severity: CurrentSeverity,
            PrecipitationKind: CurrentPrecipitationKind,
            TemperatureC: CurrentTemperatureC,
            HumidityPercent: CurrentHumidityPercent,
            WindSpeedKph: CurrentWindSpeedKph,
            CloudCoveragePercent: CurrentCloudCoveragePercent,
            PressureHpa: CurrentPressureHpa);

        public CityPopulationEnvironment? Environment =>
            EnvironmentClimateZone.HasValue &&
            EnvironmentHemisphere.HasValue &&
            EnvironmentUtcOffsetMinutes.HasValue
                ? CityPopulationEnvironment.Create(
                    cityId: CityId,
                    climateZone: EnvironmentClimateZone.Value,
                    hemisphere: EnvironmentHemisphere.Value,
                    utcOffsetMinutes: EnvironmentUtcOffsetMinutes.Value,
                    createdAtUtc: OccurredAtUtc)
                : null;

        public static CityPopulationPendingWeatherImpact Create(
            Guid impactId,
            CityId cityId,
            DateOnly currentDate,
            WeatherImpactProfile previousWeather,
            WeatherImpactProfile currentWeather,
            CityPopulationEnvironment? environment,
            DateTimeOffset occurredAtUtc)
        {
            return new CityPopulationPendingWeatherImpact(
                impactId: impactId,
                cityId: cityId,
                currentDate: currentDate,
                previousWeather: previousWeather,
                currentWeather: currentWeather,
                environment: environment,
                occurredAtUtc: occurredAtUtc);
        }

        private void SetPreviousWeather(WeatherImpactProfile weather)
        {
            PreviousType = weather.Type;
            PreviousSeverity = weather.Severity;
            PreviousPrecipitationKind = weather.PrecipitationKind;
            PreviousTemperatureC = weather.TemperatureC;
            PreviousHumidityPercent = weather.HumidityPercent;
            PreviousWindSpeedKph = weather.WindSpeedKph;
            PreviousCloudCoveragePercent = weather.CloudCoveragePercent;
            PreviousPressureHpa = weather.PressureHpa;
        }

        private void SetCurrentWeather(WeatherImpactProfile weather)
        {
            CurrentType = weather.Type;
            CurrentSeverity = weather.Severity;
            CurrentPrecipitationKind = weather.PrecipitationKind;
            CurrentTemperatureC = weather.TemperatureC;
            CurrentHumidityPercent = weather.HumidityPercent;
            CurrentWindSpeedKph = weather.WindSpeedKph;
            CurrentCloudCoveragePercent = weather.CloudCoveragePercent;
            CurrentPressureHpa = weather.PressureHpa;
        }

        private static void EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: paramName);
        }
    }
}
