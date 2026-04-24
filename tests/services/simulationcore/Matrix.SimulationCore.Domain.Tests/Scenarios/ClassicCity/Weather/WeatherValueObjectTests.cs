using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Weather;

public sealed class WeatherValueObjectTests
{
    [Fact]
    public void TemperatureC_AcceptsBoundaries_AndRejectsOutOfRangeValues()
    {
        Assert.Equal(TemperatureC.Min, TemperatureC.From(TemperatureC.Min).Value);
        Assert.Equal(TemperatureC.Max, TemperatureC.From(TemperatureC.Max).Value);
        var sample = TemperatureC.From(18.5m);

        Assert.Equal(18.5m, sample.Value);
        Assert.Equal(sample.Value.ToString("0.##"), sample.ToString());

        var belowMin = Assert.Throws<DomainException>(() => TemperatureC.From(TemperatureC.Min - 1m));
        var aboveMax = Assert.Throws<DomainException>(() => TemperatureC.From(TemperatureC.Max + 1m));

        Assert.Equal("SimulationCore.Weather.Temperature.OutOfRange", belowMin.Code);
        Assert.Equal("SimulationCore.Weather.Temperature.OutOfRange", aboveMax.Code);
    }

    [Fact]
    public void HumidityPercent_AcceptsBoundaries_AndRejectsOutOfRangeValues()
    {
        Assert.Equal(HumidityPercent.Min, HumidityPercent.From(HumidityPercent.Min).Value);
        Assert.Equal(HumidityPercent.Max, HumidityPercent.From(HumidityPercent.Max).Value);
        var sample = HumidityPercent.From(55.5m);

        Assert.Equal(55.5m, sample.Value);
        Assert.Equal(sample.Value.ToString("0.##"), sample.ToString());

        var belowMin = Assert.Throws<DomainException>(() => HumidityPercent.From(HumidityPercent.Min - 1m));
        var aboveMax = Assert.Throws<DomainException>(() => HumidityPercent.From(HumidityPercent.Max + 1m));

        Assert.Equal("SimulationCore.Weather.Humidity.OutOfRange", belowMin.Code);
        Assert.Equal("SimulationCore.Weather.Humidity.OutOfRange", aboveMax.Code);
    }

    [Fact]
    public void CloudCoveragePercent_AcceptsBoundaries_AndRejectsOutOfRangeValues()
    {
        Assert.Equal(CloudCoveragePercent.Min, CloudCoveragePercent.From(CloudCoveragePercent.Min).Value);
        Assert.Equal(CloudCoveragePercent.Max, CloudCoveragePercent.From(CloudCoveragePercent.Max).Value);
        var sample = CloudCoveragePercent.From(72.25m);

        Assert.Equal(72.25m, sample.Value);
        Assert.Equal(sample.Value.ToString("0.##"), sample.ToString());

        var belowMin = Assert.Throws<DomainException>(() => CloudCoveragePercent.From(CloudCoveragePercent.Min - 1m));
        var aboveMax = Assert.Throws<DomainException>(() => CloudCoveragePercent.From(CloudCoveragePercent.Max + 1m));

        Assert.Equal("SimulationCore.Weather.CloudCoverage.OutOfRange", belowMin.Code);
        Assert.Equal("SimulationCore.Weather.CloudCoverage.OutOfRange", aboveMax.Code);
    }

    [Fact]
    public void PressureHpa_AcceptsBoundaries_AndRejectsOutOfRangeValues()
    {
        Assert.Equal(PressureHpa.Min, PressureHpa.From(PressureHpa.Min).Value);
        Assert.Equal(PressureHpa.Max, PressureHpa.From(PressureHpa.Max).Value);
        var sample = PressureHpa.From(1012.5m);

        Assert.Equal(1012.5m, sample.Value);
        Assert.Equal(sample.Value.ToString("0.##"), sample.ToString());

        var belowMin = Assert.Throws<DomainException>(() => PressureHpa.From(PressureHpa.Min - 1m));
        var aboveMax = Assert.Throws<DomainException>(() => PressureHpa.From(PressureHpa.Max + 1m));

        Assert.Equal("SimulationCore.Weather.Pressure.OutOfRange", belowMin.Code);
        Assert.Equal("SimulationCore.Weather.Pressure.OutOfRange", aboveMax.Code);
    }

    [Fact]
    public void WeatherVolatility_AcceptsBoundaries_AndRejectsOutOfRangeValues()
    {
        Assert.Equal(WeatherVolatility.Min, WeatherVolatility.From(WeatherVolatility.Min).Value);
        Assert.Equal(WeatherVolatility.Max, WeatherVolatility.From(WeatherVolatility.Max).Value);
        var sample = WeatherVolatility.From(0.125m);

        Assert.Equal(0.125m, sample.Value);
        Assert.Equal(sample.Value.ToString("0.###"), sample.ToString());

        var belowMin = Assert.Throws<DomainException>(() => WeatherVolatility.From(WeatherVolatility.Min - 0.001m));
        var aboveMax = Assert.Throws<DomainException>(() => WeatherVolatility.From(WeatherVolatility.Max + 0.001m));

        Assert.Equal("SimulationCore.Weather.Volatility.OutOfRange", belowMin.Code);
        Assert.Equal("SimulationCore.Weather.Volatility.OutOfRange", aboveMax.Code);
    }

    [Fact]
    public void WindSpeedKph_AcceptsBoundaries_AndRejectsOutOfRangeValues()
    {
        Assert.Equal(WindSpeedKph.Min, WindSpeedKph.From(WindSpeedKph.Min).Value);
        Assert.Equal(WindSpeedKph.Max, WindSpeedKph.From(WindSpeedKph.Max).Value);
        var sample = WindSpeedKph.From(18.75m);

        Assert.Equal(18.75m, sample.Value);
        Assert.Equal(sample.Value.ToString("0.##"), sample.ToString());

        var belowMin = Assert.Throws<DomainException>(() => WindSpeedKph.From(WindSpeedKph.Min - 1m));
        var aboveMax = Assert.Throws<DomainException>(() => WindSpeedKph.From(WindSpeedKph.Max + 1m));

        Assert.Equal("SimulationCore.Weather.WindSpeed.OutOfRange", belowMin.Code);
        Assert.Equal("SimulationCore.Weather.WindSpeed.OutOfRange", aboveMax.Code);
    }
}
