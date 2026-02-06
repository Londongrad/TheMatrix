using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class CityPopulationWeatherExposureState
    {
        private CityPopulationWeatherExposureState() { }

        private CityPopulationWeatherExposureState(
            CityId cityId,
            WeatherImpactProfile currentWeather,
            DateTimeOffset currentWeatherEffectiveAtSimTimeUtc,
            DateTimeOffset lastWeatherOccurredOnUtc,
            DateTimeOffset lastExposureProcessedAtSimTimeUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(currentWeatherEffectiveAtSimTimeUtc, nameof(currentWeatherEffectiveAtSimTimeUtc));
            EnsureUtc(lastWeatherOccurredOnUtc, nameof(lastWeatherOccurredOnUtc));
            EnsureUtc(lastExposureProcessedAtSimTimeUtc, nameof(lastExposureProcessedAtSimTimeUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
            currentWeather = GuardHelper.AgainstNull(
                value: currentWeather,
                propertyName: nameof(currentWeather));

            CityId = cityId;
            CurrentType = currentWeather.Type;
            CurrentSeverity = currentWeather.Severity;
            CurrentPrecipitationKind = currentWeather.PrecipitationKind;
            CurrentTemperatureC = currentWeather.TemperatureC;
            CurrentHumidityPercent = currentWeather.HumidityPercent;
            CurrentWindSpeedKph = currentWeather.WindSpeedKph;
            CurrentCloudCoveragePercent = currentWeather.CloudCoveragePercent;
            CurrentPressureHpa = currentWeather.PressureHpa;
            CurrentWeatherEffectiveAtSimTimeUtc = currentWeatherEffectiveAtSimTimeUtc;
            LastWeatherOccurredOnUtc = lastWeatherOccurredOnUtc;
            LastExposureProcessedAtSimTimeUtc = lastExposureProcessedAtSimTimeUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }

        public PopulationWeatherType CurrentType { get; private set; }
        public PopulationWeatherSeverity CurrentSeverity { get; private set; }
        public PopulationPrecipitationKind CurrentPrecipitationKind { get; private set; }
        public decimal CurrentTemperatureC { get; private set; }
        public decimal CurrentHumidityPercent { get; private set; }
        public decimal CurrentWindSpeedKph { get; private set; }
        public decimal CurrentCloudCoveragePercent { get; private set; }
        public decimal CurrentPressureHpa { get; private set; }
        public DateTimeOffset CurrentWeatherEffectiveAtSimTimeUtc { get; private set; }

        public PopulationWeatherType? PreviousType { get; private set; }
        public PopulationWeatherSeverity? PreviousSeverity { get; private set; }
        public PopulationPrecipitationKind? PreviousPrecipitationKind { get; private set; }
        public decimal? PreviousTemperatureC { get; private set; }
        public decimal? PreviousHumidityPercent { get; private set; }
        public decimal? PreviousWindSpeedKph { get; private set; }
        public decimal? PreviousCloudCoveragePercent { get; private set; }
        public decimal? PreviousPressureHpa { get; private set; }
        public DateTimeOffset? PreviousWeatherEffectiveAtSimTimeUtc { get; private set; }

        public PopulationWeatherType? RecoverySourceType { get; private set; }
        public PopulationWeatherSeverity? RecoverySourceSeverity { get; private set; }
        public PopulationPrecipitationKind? RecoverySourcePrecipitationKind { get; private set; }
        public decimal? RecoverySourceTemperatureC { get; private set; }
        public decimal? RecoverySourceHumidityPercent { get; private set; }
        public decimal? RecoverySourceWindSpeedKph { get; private set; }
        public decimal? RecoverySourceCloudCoveragePercent { get; private set; }
        public decimal? RecoverySourcePressureHpa { get; private set; }
        public DateTimeOffset? RecoveryStartedAtSimTimeUtc { get; private set; }

        public DateTimeOffset LastWeatherOccurredOnUtc { get; private set; }
        public DateTimeOffset LastExposureProcessedAtSimTimeUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public WeatherImpactProfile CurrentWeather => new(
            Type: CurrentType,
            Severity: CurrentSeverity,
            PrecipitationKind: CurrentPrecipitationKind,
            TemperatureC: CurrentTemperatureC,
            HumidityPercent: CurrentHumidityPercent,
            WindSpeedKph: CurrentWindSpeedKph,
            CloudCoveragePercent: CurrentCloudCoveragePercent,
            PressureHpa: CurrentPressureHpa);

        public bool HasPreviousWeather =>
            PreviousType.HasValue &&
            PreviousSeverity.HasValue &&
            PreviousPrecipitationKind.HasValue &&
            PreviousTemperatureC.HasValue &&
            PreviousHumidityPercent.HasValue &&
            PreviousWindSpeedKph.HasValue &&
            PreviousCloudCoveragePercent.HasValue &&
            PreviousPressureHpa.HasValue &&
            PreviousWeatherEffectiveAtSimTimeUtc.HasValue;

        public WeatherImpactProfile? PreviousWeather => !HasPreviousWeather
            ? null
            : new WeatherImpactProfile(
                Type: PreviousType!.Value,
                Severity: PreviousSeverity!.Value,
                PrecipitationKind: PreviousPrecipitationKind!.Value,
                TemperatureC: PreviousTemperatureC!.Value,
                HumidityPercent: PreviousHumidityPercent!.Value,
                WindSpeedKph: PreviousWindSpeedKph!.Value,
                CloudCoveragePercent: PreviousCloudCoveragePercent!.Value,
                PressureHpa: PreviousPressureHpa!.Value);

        public bool HasRecoverySource =>
            RecoverySourceType.HasValue &&
            RecoverySourceSeverity.HasValue &&
            RecoverySourcePrecipitationKind.HasValue &&
            RecoverySourceTemperatureC.HasValue &&
            RecoverySourceHumidityPercent.HasValue &&
            RecoverySourceWindSpeedKph.HasValue &&
            RecoverySourceCloudCoveragePercent.HasValue &&
            RecoverySourcePressureHpa.HasValue &&
            RecoveryStartedAtSimTimeUtc.HasValue;

        public WeatherImpactProfile? RecoverySourceWeather => !HasRecoverySource
            ? null
            : new WeatherImpactProfile(
                Type: RecoverySourceType!.Value,
                Severity: RecoverySourceSeverity!.Value,
                PrecipitationKind: RecoverySourcePrecipitationKind!.Value,
                TemperatureC: RecoverySourceTemperatureC!.Value,
                HumidityPercent: RecoverySourceHumidityPercent!.Value,
                WindSpeedKph: RecoverySourceWindSpeedKph!.Value,
                CloudCoveragePercent: RecoverySourceCloudCoveragePercent!.Value,
                PressureHpa: RecoverySourcePressureHpa!.Value);

        public static CityPopulationWeatherExposureState Create(
            CityId cityId,
            WeatherImpactProfile currentWeather,
            DateTimeOffset currentWeatherEffectiveAtSimTimeUtc,
            DateTimeOffset occurredOnUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationWeatherExposureState(
                cityId: cityId,
                currentWeather: currentWeather,
                currentWeatherEffectiveAtSimTimeUtc: currentWeatherEffectiveAtSimTimeUtc,
                lastWeatherOccurredOnUtc: occurredOnUtc,
                lastExposureProcessedAtSimTimeUtc: currentWeatherEffectiveAtSimTimeUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public bool CanApplyWeatherUpdate(
            DateTimeOffset atSimTimeUtc,
            DateTimeOffset occurredOnUtc)
        {
            EnsureUtc(atSimTimeUtc, nameof(atSimTimeUtc));
            EnsureUtc(occurredOnUtc, nameof(occurredOnUtc));

            if (atSimTimeUtc < CurrentWeatherEffectiveAtSimTimeUtc)
                return false;

            return atSimTimeUtc != CurrentWeatherEffectiveAtSimTimeUtc ||
                   occurredOnUtc > LastWeatherOccurredOnUtc;
        }

        public void ApplyWeatherUpdate(
            WeatherImpactProfile currentWeather,
            DateTimeOffset currentWeatherEffectiveAtSimTimeUtc,
            DateTimeOffset occurredOnUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(currentWeatherEffectiveAtSimTimeUtc, nameof(currentWeatherEffectiveAtSimTimeUtc));
            EnsureUtc(occurredOnUtc, nameof(occurredOnUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
            currentWeather = GuardHelper.AgainstNull(
                value: currentWeather,
                propertyName: nameof(currentWeather));

            GuardHelper.Ensure(
                condition: CanApplyWeatherUpdate(
                    atSimTimeUtc: currentWeatherEffectiveAtSimTimeUtc,
                    occurredOnUtc: occurredOnUtc),
                value: currentWeatherEffectiveAtSimTimeUtc,
                errorFactory: (_, propertyName) => DomainErrorsFactory.CityPopulationWeatherExposureStaleUpdate(propertyName),
                propertyName: nameof(currentWeatherEffectiveAtSimTimeUtc));

            WeatherImpactProfile previousCurrentWeather = CurrentWeather;

            PreviousType = CurrentType;
            PreviousSeverity = CurrentSeverity;
            PreviousPrecipitationKind = CurrentPrecipitationKind;
            PreviousTemperatureC = CurrentTemperatureC;
            PreviousHumidityPercent = CurrentHumidityPercent;
            PreviousWindSpeedKph = CurrentWindSpeedKph;
            PreviousCloudCoveragePercent = CurrentCloudCoveragePercent;
            PreviousPressureHpa = CurrentPressureHpa;
            PreviousWeatherEffectiveAtSimTimeUtc = CurrentWeatherEffectiveAtSimTimeUtc;

            CurrentType = currentWeather.Type;
            CurrentSeverity = currentWeather.Severity;
            CurrentPrecipitationKind = currentWeather.PrecipitationKind;
            CurrentTemperatureC = currentWeather.TemperatureC;
            CurrentHumidityPercent = currentWeather.HumidityPercent;
            CurrentWindSpeedKph = currentWeather.WindSpeedKph;
            CurrentCloudCoveragePercent = currentWeather.CloudCoveragePercent;
            CurrentPressureHpa = currentWeather.PressureHpa;
            CurrentWeatherEffectiveAtSimTimeUtc = currentWeatherEffectiveAtSimTimeUtc;
            LastWeatherOccurredOnUtc = occurredOnUtc;
            UpdatedAtUtc = updatedAtUtc;

            if (CityWeatherExposureRules.IsAdverseExposureWeather(previousCurrentWeather) &&
                CityWeatherExposureRules.IsRecoveryWeather(currentWeather))
            {
                SetRecoverySource(
                    recoverySourceWeather: previousCurrentWeather,
                    recoveryStartedAtSimTimeUtc: currentWeatherEffectiveAtSimTimeUtc);
                return;
            }

            if (!(HasRecoverySource && CityWeatherExposureRules.IsRecoveryWeather(currentWeather)))
                ClearRecoverySource();
        }

        public void MarkExposureProcessed(
            DateTimeOffset processedAtSimTimeUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(processedAtSimTimeUtc, nameof(processedAtSimTimeUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            GuardHelper.Ensure(
                condition: processedAtSimTimeUtc >= LastExposureProcessedAtSimTimeUtc,
                value: processedAtSimTimeUtc,
                errorFactory: (value, propertyName) => DomainErrorsFactory.CityPopulationWeatherExposureProcessedAtCannotMoveBackwards(
                    value: value,
                    previous: LastExposureProcessedAtSimTimeUtc,
                    propertyName: propertyName),
                propertyName: nameof(processedAtSimTimeUtc));

            LastExposureProcessedAtSimTimeUtc = processedAtSimTimeUtc;
            UpdatedAtUtc = updatedAtUtc;

            if (processedAtSimTimeUtc >= CurrentWeatherEffectiveAtSimTimeUtc)
                ClearPreviousWeather();
        }

        private void ClearPreviousWeather()
        {
            PreviousType = null;
            PreviousSeverity = null;
            PreviousPrecipitationKind = null;
            PreviousTemperatureC = null;
            PreviousHumidityPercent = null;
            PreviousWindSpeedKph = null;
            PreviousCloudCoveragePercent = null;
            PreviousPressureHpa = null;
            PreviousWeatherEffectiveAtSimTimeUtc = null;
        }

        private void SetRecoverySource(
            WeatherImpactProfile recoverySourceWeather,
            DateTimeOffset recoveryStartedAtSimTimeUtc)
        {
            EnsureUtc(recoveryStartedAtSimTimeUtc, nameof(recoveryStartedAtSimTimeUtc));
            recoverySourceWeather = GuardHelper.AgainstNull(
                value: recoverySourceWeather,
                propertyName: nameof(recoverySourceWeather));

            RecoverySourceType = recoverySourceWeather.Type;
            RecoverySourceSeverity = recoverySourceWeather.Severity;
            RecoverySourcePrecipitationKind = recoverySourceWeather.PrecipitationKind;
            RecoverySourceTemperatureC = recoverySourceWeather.TemperatureC;
            RecoverySourceHumidityPercent = recoverySourceWeather.HumidityPercent;
            RecoverySourceWindSpeedKph = recoverySourceWeather.WindSpeedKph;
            RecoverySourceCloudCoveragePercent = recoverySourceWeather.CloudCoveragePercent;
            RecoverySourcePressureHpa = recoverySourceWeather.PressureHpa;
            RecoveryStartedAtSimTimeUtc = recoveryStartedAtSimTimeUtc;
        }

        private void ClearRecoverySource()
        {
            RecoverySourceType = null;
            RecoverySourceSeverity = null;
            RecoverySourcePrecipitationKind = null;
            RecoverySourceTemperatureC = null;
            RecoverySourceHumidityPercent = null;
            RecoverySourceWindSpeedKph = null;
            RecoverySourceCloudCoveragePercent = null;
            RecoverySourcePressureHpa = null;
            RecoveryStartedAtSimTimeUtc = null;
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
