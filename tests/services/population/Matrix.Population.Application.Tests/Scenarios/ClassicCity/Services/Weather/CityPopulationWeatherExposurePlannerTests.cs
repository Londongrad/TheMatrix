using Matrix.Population.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.Services.Weather
{
    public sealed class CityPopulationWeatherExposurePlannerTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));

        private static readonly DateTimeOffset BaseTime = new(
            year: 2030,
            month: 1,
            day: 1,
            hour: 8,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public void ShouldAdvanceCheckpoint_WhenStateIsNull_ReturnsFalse()
        {
            bool result = CityPopulationWeatherExposurePlanner.ShouldAdvanceCheckpoint(
                weatherExposureState: null,
                fromSimTimeUtc: BaseTime,
                toSimTimeUtc: BaseTime.AddHours(1));

            Assert.False(result);
        }

        [Fact]
        public void ShouldAdvanceCheckpoint_WhenToTimeDoesNotPassEffectiveCheckpoint_ReturnsFalse()
        {
            DateTimeOffset checkpoint = BaseTime.AddHours(1);
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: CreateAdverseWeather(),
                currentWeatherEffectiveAtSimTimeUtc: checkpoint);

            bool result = CityPopulationWeatherExposurePlanner.ShouldAdvanceCheckpoint(
                weatherExposureState: state,
                fromSimTimeUtc: BaseTime,
                toSimTimeUtc: checkpoint);

            Assert.False(result);
        }

        [Fact]
        public void ShouldAdvanceCheckpoint_WhenToTimePassesEffectiveCheckpoint_ReturnsTrue()
        {
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: CreateAdverseWeather(),
                currentWeatherEffectiveAtSimTimeUtc: BaseTime);

            bool result = CityPopulationWeatherExposurePlanner.ShouldAdvanceCheckpoint(
                weatherExposureState: state,
                fromSimTimeUtc: BaseTime.AddHours(-1),
                toSimTimeUtc: BaseTime.AddHours(1));

            Assert.True(result);
        }

        [Fact]
        public void BuildSegments_WhenNoTimeNeedsProcessing_ReturnsEmptyList()
        {
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: CreateAdverseWeather(),
                currentWeatherEffectiveAtSimTimeUtc: BaseTime);

            List<CityWeatherExposureSegment> segments = CityPopulationWeatherExposurePlanner.BuildSegments(
                weatherExposureState: state,
                fromSimTimeUtc: BaseTime.AddMinutes(30),
                toSimTimeUtc: BaseTime.AddMinutes(30));

            Assert.Empty(segments);
        }

        [Fact]
        public void BuildSegments_WhenCurrentWeatherIsAdverse_IncludesCurrentAdverseExposureSegment()
        {
            WeatherImpactProfile adverseWeather = CreateAdverseWeather();
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: adverseWeather,
                currentWeatherEffectiveAtSimTimeUtc: BaseTime);
            DateTimeOffset from = BaseTime.AddMinutes(30);
            DateTimeOffset to = BaseTime.AddHours(1);

            List<CityWeatherExposureSegment> segments = CityPopulationWeatherExposurePlanner.BuildSegments(
                weatherExposureState: state,
                fromSimTimeUtc: from,
                toSimTimeUtc: to);

            CityWeatherExposureSegment segment = Assert.Single(segments);

            Assert.Equal(
                expected: CityWeatherExposureKind.Adverse,
                actual: segment.Kind);
            Assert.Equal(
                expected: from,
                actual: segment.IntervalStartSimTimeUtc);
            Assert.Equal(
                expected: to,
                actual: segment.IntervalEndSimTimeUtc);
            Assert.Equal(
                expected: BaseTime,
                actual: segment.EffectStartedAtSimTimeUtc);
            Assert.Equal(
                expected: adverseWeather,
                actual: segment.Weather);
            Assert.Null(segment.SourceWeather);
        }

        [Fact]
        public void
            BuildSegments_WhenPreviousWeatherIsAdverseBeforeCurrentEffectiveTime_IncludesPreviousAdverseSegment()
        {
            WeatherImpactProfile previousAdverseWeather = CreateAdverseWeather();
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: previousAdverseWeather,
                currentWeatherEffectiveAtSimTimeUtc: BaseTime);
            state.ApplyWeatherUpdate(
                currentWeather: CreateNeutralWeather(),
                currentWeatherEffectiveAtSimTimeUtc: BaseTime.AddHours(2),
                occurredOnUtc: BaseTime.AddHours(2),
                updatedAtUtc: BaseTime.AddHours(2));
            DateTimeOffset from = BaseTime.AddHours(1);
            DateTimeOffset to = BaseTime.AddHours(1)
               .AddMinutes(30);

            List<CityWeatherExposureSegment> segments = CityPopulationWeatherExposurePlanner.BuildSegments(
                weatherExposureState: state,
                fromSimTimeUtc: from,
                toSimTimeUtc: to);

            CityWeatherExposureSegment segment = Assert.Single(segments);

            Assert.Equal(
                expected: CityWeatherExposureKind.Adverse,
                actual: segment.Kind);
            Assert.Equal(
                expected: previousAdverseWeather,
                actual: segment.Weather);
            Assert.Equal(
                expected: from,
                actual: segment.IntervalStartSimTimeUtc);
            Assert.Equal(
                expected: to,
                actual: segment.IntervalEndSimTimeUtc);
            Assert.Equal(
                expected: BaseTime,
                actual: segment.EffectStartedAtSimTimeUtc);
        }

        [Fact]
        public void BuildSegments_WhenPreviousAndCurrentWeatherAreAdverse_SplitsSegmentsChronologically()
        {
            WeatherImpactProfile previousAdverseWeather = CreateAdverseWeather();
            WeatherImpactProfile currentAdverseWeather = CreateAdverseWeather(
                type: PopulationWeatherType.Heatwave,
                precipitationKind: PopulationPrecipitationKind.None,
                temperatureC: 34m,
                windSpeedKph: 8m);
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: previousAdverseWeather,
                currentWeatherEffectiveAtSimTimeUtc: BaseTime);
            DateTimeOffset currentEffectiveAt = BaseTime.AddHours(2);
            state.ApplyWeatherUpdate(
                currentWeather: currentAdverseWeather,
                currentWeatherEffectiveAtSimTimeUtc: currentEffectiveAt,
                occurredOnUtc: currentEffectiveAt,
                updatedAtUtc: currentEffectiveAt);
            DateTimeOffset from = BaseTime.AddHours(1)
               .AddMinutes(30);
            DateTimeOffset to = BaseTime.AddHours(2)
               .AddMinutes(30);

            List<CityWeatherExposureSegment> segments = CityPopulationWeatherExposurePlanner.BuildSegments(
                weatherExposureState: state,
                fromSimTimeUtc: from,
                toSimTimeUtc: to);

            Assert.Equal(
                expected: 2,
                actual: segments.Count);
            Assert.Equal(
                expected: previousAdverseWeather,
                actual: segments[0].Weather);
            Assert.Equal(
                expected: currentAdverseWeather,
                actual: segments[1].Weather);
            Assert.Equal(
                expected: from,
                actual: segments[0].IntervalStartSimTimeUtc);
            Assert.Equal(
                expected: currentEffectiveAt,
                actual: segments[0].IntervalEndSimTimeUtc);
            Assert.Equal(
                expected: currentEffectiveAt,
                actual: segments[1].IntervalStartSimTimeUtc);
            Assert.Equal(
                expected: to,
                actual: segments[1].IntervalEndSimTimeUtc);
            Assert.True(segments[0].IntervalEndSimTimeUtc <= segments[1].IntervalStartSimTimeUtc);
        }

        [Fact]
        public void BuildSegments_WhenCurrentWeatherIsRecoveryAndSourceExists_IncludesRecoverySegment()
        {
            WeatherImpactProfile adverseWeather = CreateAdverseWeather();
            WeatherImpactProfile recoveryWeather = CreateRecoveryWeather();
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: adverseWeather,
                currentWeatherEffectiveAtSimTimeUtc: BaseTime);
            DateTimeOffset recoveryStartedAt = BaseTime.AddHours(2);
            state.ApplyWeatherUpdate(
                currentWeather: recoveryWeather,
                currentWeatherEffectiveAtSimTimeUtc: recoveryStartedAt,
                occurredOnUtc: recoveryStartedAt,
                updatedAtUtc: recoveryStartedAt);
            DateTimeOffset to = recoveryStartedAt.AddHours(1);

            List<CityWeatherExposureSegment> segments = CityPopulationWeatherExposurePlanner.BuildSegments(
                weatherExposureState: state,
                fromSimTimeUtc: recoveryStartedAt,
                toSimTimeUtc: to);

            CityWeatherExposureSegment segment = Assert.Single(segments);

            Assert.Equal(
                expected: CityWeatherExposureKind.Recovery,
                actual: segment.Kind);
            Assert.Equal(
                expected: recoveryStartedAt,
                actual: segment.IntervalStartSimTimeUtc);
            Assert.Equal(
                expected: to,
                actual: segment.IntervalEndSimTimeUtc);
            Assert.Equal(
                expected: recoveryStartedAt,
                actual: segment.EffectStartedAtSimTimeUtc);
            Assert.Equal(
                expected: recoveryWeather,
                actual: segment.Weather);
            Assert.NotNull(segment.SourceWeather);
            Assert.Equal(
                expected: adverseWeather,
                actual: segment.SourceWeather);
        }

        [Fact]
        public void BuildSegments_WhenRecoveryWeatherHasNoSource_ReturnsEmptyList()
        {
            CityPopulationWeatherExposureState state = CreateWeatherExposureState(
                currentWeather: CreateRecoveryWeather(),
                currentWeatherEffectiveAtSimTimeUtc: BaseTime);

            List<CityWeatherExposureSegment> segments = CityPopulationWeatherExposurePlanner.BuildSegments(
                weatherExposureState: state,
                fromSimTimeUtc: BaseTime.AddHours(1),
                toSimTimeUtc: BaseTime.AddHours(2));

            Assert.Empty(segments);
        }

        private static CityPopulationWeatherExposureState CreateWeatherExposureState(
            WeatherImpactProfile currentWeather,
            DateTimeOffset currentWeatherEffectiveAtSimTimeUtc)
        {
            return CityPopulationWeatherExposureState.Create(
                cityId: TestCityId,
                currentWeather: currentWeather,
                currentWeatherEffectiveAtSimTimeUtc: currentWeatherEffectiveAtSimTimeUtc,
                occurredOnUtc: currentWeatherEffectiveAtSimTimeUtc,
                updatedAtUtc: currentWeatherEffectiveAtSimTimeUtc);
        }

        private static WeatherImpactProfile CreateAdverseWeather(
            PopulationWeatherType type = PopulationWeatherType.Storm,
            PopulationWeatherSeverity severity = PopulationWeatherSeverity.Moderate,
            PopulationPrecipitationKind precipitationKind = PopulationPrecipitationKind.Rain,
            decimal temperatureC = 12m,
            decimal windSpeedKph = 32m)
        {
            return new WeatherImpactProfile(
                Type: type,
                Severity: severity,
                PrecipitationKind: precipitationKind,
                TemperatureC: temperatureC,
                HumidityPercent: 75m,
                WindSpeedKph: windSpeedKph,
                CloudCoveragePercent: 82m,
                PressureHpa: 1002m);
        }

        private static WeatherImpactProfile CreateRecoveryWeather()
        {
            return new WeatherImpactProfile(
                Type: PopulationWeatherType.Clear,
                Severity: PopulationWeatherSeverity.Mild,
                PrecipitationKind: PopulationPrecipitationKind.None,
                TemperatureC: 20m,
                HumidityPercent: 48m,
                WindSpeedKph: 12m,
                CloudCoveragePercent: 10m,
                PressureHpa: 1014m);
        }

        private static WeatherImpactProfile CreateNeutralWeather()
        {
            return new WeatherImpactProfile(
                Type: PopulationWeatherType.Rain,
                Severity: PopulationWeatherSeverity.Mild,
                PrecipitationKind: PopulationPrecipitationKind.Rain,
                TemperatureC: 14m,
                HumidityPercent: 72m,
                WindSpeedKph: 10m,
                CloudCoveragePercent: 70m,
                PressureHpa: 1008m);
        }
    }
}
