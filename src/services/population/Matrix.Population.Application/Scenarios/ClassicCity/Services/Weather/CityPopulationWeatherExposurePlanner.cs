using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.Weather
{
    internal static class CityPopulationWeatherExposurePlanner
    {
        public static bool ShouldAdvanceCheckpoint(
            CityPopulationWeatherExposureState? weatherExposureState,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            if (weatherExposureState is null)
                return false;

            DateTimeOffset effectiveFrom = Max(
                left: fromSimTimeUtc,
                right: weatherExposureState.LastExposureProcessedAtSimTimeUtc);

            return toSimTimeUtc > effectiveFrom;
        }

        public static List<CityWeatherExposureSegment> BuildSegments(
            CityPopulationWeatherExposureState weatherExposureState,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc)
        {
            var segments = new List<CityWeatherExposureSegment>();
            DateTimeOffset effectiveFrom = Max(
                left: fromSimTimeUtc,
                right: weatherExposureState.LastExposureProcessedAtSimTimeUtc);

            if (toSimTimeUtc <= effectiveFrom)
                return segments;

            if (weatherExposureState.HasPreviousWeather &&
                weatherExposureState.PreviousWeather is WeatherImpactProfile previousWeather &&
                weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.HasValue &&
                effectiveFrom < weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc)
            {
                DateTimeOffset previousStart = Max(
                    left: effectiveFrom,
                    right: weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.Value);
                DateTimeOffset previousEnd = Min(
                    left: toSimTimeUtc,
                    right: weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);

                if (previousEnd > previousStart && CityWeatherExposureRules.IsAdverseExposureWeather(previousWeather))
                    segments.Add(
                        new CityWeatherExposureSegment(
                            Kind: CityWeatherExposureKind.Adverse,
                            Weather: previousWeather,
                            EffectStartedAtSimTimeUtc: weatherExposureState.PreviousWeatherEffectiveAtSimTimeUtc.Value,
                            IntervalStartSimTimeUtc: previousStart,
                            IntervalEndSimTimeUtc: previousEnd));
            }

            DateTimeOffset currentStart = Max(
                left: effectiveFrom,
                right: weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc);

            if (toSimTimeUtc > currentStart &&
                CityWeatherExposureRules.IsAdverseExposureWeather(weatherExposureState.CurrentWeather))
                segments.Add(
                    new CityWeatherExposureSegment(
                        Kind: CityWeatherExposureKind.Adverse,
                        Weather: weatherExposureState.CurrentWeather,
                        EffectStartedAtSimTimeUtc: weatherExposureState.CurrentWeatherEffectiveAtSimTimeUtc,
                        IntervalStartSimTimeUtc: currentStart,
                        IntervalEndSimTimeUtc: toSimTimeUtc));

            if (toSimTimeUtc > currentStart &&
                weatherExposureState.HasRecoverySource &&
                weatherExposureState.RecoverySourceWeather is WeatherImpactProfile recoverySourceWeather &&
                weatherExposureState.RecoveryStartedAtSimTimeUtc.HasValue &&
                CityWeatherExposureRules.IsRecoveryWeather(weatherExposureState.CurrentWeather))
            {
                DateTimeOffset recoveryStart = Max(
                    left: currentStart,
                    right: weatherExposureState.RecoveryStartedAtSimTimeUtc.Value);

                if (toSimTimeUtc > recoveryStart)
                    segments.Add(
                        new CityWeatherExposureSegment(
                            Kind: CityWeatherExposureKind.Recovery,
                            Weather: weatherExposureState.CurrentWeather,
                            EffectStartedAtSimTimeUtc: weatherExposureState.RecoveryStartedAtSimTimeUtc.Value,
                            IntervalStartSimTimeUtc: recoveryStart,
                            IntervalEndSimTimeUtc: toSimTimeUtc,
                            SourceWeather: recoverySourceWeather));
            }

            return segments;
        }

        private static DateTimeOffset Max(
            DateTimeOffset left,
            DateTimeOffset right)
        {
            return left >= right
                ? left
                : right;
        }

        private static DateTimeOffset Min(
            DateTimeOffset left,
            DateTimeOffset right)
        {
            return left <= right
                ? left
                : right;
        }
    }
}
