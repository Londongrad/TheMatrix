using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather
{
    public sealed class WeatherValueObjectTests
    {
        [Fact]
        public void TemperatureC_AcceptsBoundaries_AndRejectsOutOfRangeValues()
        {
            Assert.Equal(
                expected: TemperatureC.Min,
                actual: TemperatureC.From(TemperatureC.Min)
                   .Value);
            Assert.Equal(
                expected: TemperatureC.Max,
                actual: TemperatureC.From(TemperatureC.Max)
                   .Value);
            var sample = TemperatureC.From(18.5m);

            Assert.Equal(
                expected: 18.5m,
                actual: sample.Value);
            Assert.Equal(
                expected: sample.Value.ToString("0.##"),
                actual: sample.ToString());

            DomainException belowMin = Assert.Throws<DomainException>(() => TemperatureC.From(TemperatureC.Min - 1m));
            DomainException aboveMax = Assert.Throws<DomainException>(() => TemperatureC.From(TemperatureC.Max + 1m));

            Assert.Equal(
                expected: "SimulationCore.Weather.Temperature.OutOfRange",
                actual: belowMin.Code);
            Assert.Equal(
                expected: "SimulationCore.Weather.Temperature.OutOfRange",
                actual: aboveMax.Code);
        }

        [Fact]
        public void HumidityPercent_AcceptsBoundaries_AndRejectsOutOfRangeValues()
        {
            Assert.Equal(
                expected: HumidityPercent.Min,
                actual: HumidityPercent.From(HumidityPercent.Min)
                   .Value);
            Assert.Equal(
                expected: HumidityPercent.Max,
                actual: HumidityPercent.From(HumidityPercent.Max)
                   .Value);
            var sample = HumidityPercent.From(55.5m);

            Assert.Equal(
                expected: 55.5m,
                actual: sample.Value);
            Assert.Equal(
                expected: sample.Value.ToString("0.##"),
                actual: sample.ToString());

            DomainException belowMin =
                Assert.Throws<DomainException>(() => HumidityPercent.From(HumidityPercent.Min - 1m));
            DomainException aboveMax =
                Assert.Throws<DomainException>(() => HumidityPercent.From(HumidityPercent.Max + 1m));

            Assert.Equal(
                expected: "SimulationCore.Weather.Humidity.OutOfRange",
                actual: belowMin.Code);
            Assert.Equal(
                expected: "SimulationCore.Weather.Humidity.OutOfRange",
                actual: aboveMax.Code);
        }

        [Fact]
        public void CloudCoveragePercent_AcceptsBoundaries_AndRejectsOutOfRangeValues()
        {
            Assert.Equal(
                expected: CloudCoveragePercent.Min,
                actual: CloudCoveragePercent.From(CloudCoveragePercent.Min)
                   .Value);
            Assert.Equal(
                expected: CloudCoveragePercent.Max,
                actual: CloudCoveragePercent.From(CloudCoveragePercent.Max)
                   .Value);
            var sample = CloudCoveragePercent.From(72.25m);

            Assert.Equal(
                expected: 72.25m,
                actual: sample.Value);
            Assert.Equal(
                expected: sample.Value.ToString("0.##"),
                actual: sample.ToString());

            DomainException belowMin =
                Assert.Throws<DomainException>(() => CloudCoveragePercent.From(CloudCoveragePercent.Min - 1m));
            DomainException aboveMax =
                Assert.Throws<DomainException>(() => CloudCoveragePercent.From(CloudCoveragePercent.Max + 1m));

            Assert.Equal(
                expected: "SimulationCore.Weather.CloudCoverage.OutOfRange",
                actual: belowMin.Code);
            Assert.Equal(
                expected: "SimulationCore.Weather.CloudCoverage.OutOfRange",
                actual: aboveMax.Code);
        }

        [Fact]
        public void PressureHpa_AcceptsBoundaries_AndRejectsOutOfRangeValues()
        {
            Assert.Equal(
                expected: PressureHpa.Min,
                actual: PressureHpa.From(PressureHpa.Min)
                   .Value);
            Assert.Equal(
                expected: PressureHpa.Max,
                actual: PressureHpa.From(PressureHpa.Max)
                   .Value);
            var sample = PressureHpa.From(1012.5m);

            Assert.Equal(
                expected: 1012.5m,
                actual: sample.Value);
            Assert.Equal(
                expected: sample.Value.ToString("0.##"),
                actual: sample.ToString());

            DomainException belowMin = Assert.Throws<DomainException>(() => PressureHpa.From(PressureHpa.Min - 1m));
            DomainException aboveMax = Assert.Throws<DomainException>(() => PressureHpa.From(PressureHpa.Max + 1m));

            Assert.Equal(
                expected: "SimulationCore.Weather.Pressure.OutOfRange",
                actual: belowMin.Code);
            Assert.Equal(
                expected: "SimulationCore.Weather.Pressure.OutOfRange",
                actual: aboveMax.Code);
        }

        [Fact]
        public void WeatherVolatility_AcceptsBoundaries_AndRejectsOutOfRangeValues()
        {
            Assert.Equal(
                expected: WeatherVolatility.Min,
                actual: WeatherVolatility.From(WeatherVolatility.Min)
                   .Value);
            Assert.Equal(
                expected: WeatherVolatility.Max,
                actual: WeatherVolatility.From(WeatherVolatility.Max)
                   .Value);
            var sample = WeatherVolatility.From(0.125m);

            Assert.Equal(
                expected: 0.125m,
                actual: sample.Value);
            Assert.Equal(
                expected: sample.Value.ToString("0.###"),
                actual: sample.ToString());

            DomainException belowMin =
                Assert.Throws<DomainException>(() => WeatherVolatility.From(WeatherVolatility.Min - 0.001m));
            DomainException aboveMax =
                Assert.Throws<DomainException>(() => WeatherVolatility.From(WeatherVolatility.Max + 0.001m));

            Assert.Equal(
                expected: "SimulationCore.Weather.Volatility.OutOfRange",
                actual: belowMin.Code);
            Assert.Equal(
                expected: "SimulationCore.Weather.Volatility.OutOfRange",
                actual: aboveMax.Code);
        }

        [Fact]
        public void WindSpeedKph_AcceptsBoundaries_AndRejectsOutOfRangeValues()
        {
            Assert.Equal(
                expected: WindSpeedKph.Min,
                actual: WindSpeedKph.From(WindSpeedKph.Min)
                   .Value);
            Assert.Equal(
                expected: WindSpeedKph.Max,
                actual: WindSpeedKph.From(WindSpeedKph.Max)
                   .Value);
            var sample = WindSpeedKph.From(18.75m);

            Assert.Equal(
                expected: 18.75m,
                actual: sample.Value);
            Assert.Equal(
                expected: sample.Value.ToString("0.##"),
                actual: sample.ToString());

            DomainException belowMin = Assert.Throws<DomainException>(() => WindSpeedKph.From(WindSpeedKph.Min - 1m));
            DomainException aboveMax = Assert.Throws<DomainException>(() => WindSpeedKph.From(WindSpeedKph.Max + 1m));

            Assert.Equal(
                expected: "SimulationCore.Weather.WindSpeed.OutOfRange",
                actual: belowMin.Code);
            Assert.Equal(
                expected: "SimulationCore.Weather.WindSpeed.OutOfRange",
                actual: aboveMax.Code);
        }
    }
}
