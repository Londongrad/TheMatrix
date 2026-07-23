using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity;

public sealed class CityResidentActivityObservationTests
{
    [Fact]
    public void ForTick_SamplesInitialTickAndHourBoundariesOnly()
    {
        var start = new DateTimeOffset(2048, 5, 3, 9, 0, 0, TimeSpan.Zero);
        Assert.NotNull(CityResidentActivityObservation.ForTick(1, start, start.AddSeconds(1), true));
        for (int minute = 1; minute < 60; minute++)
            Assert.Null(CityResidentActivityObservation.ForTick(minute + 1, start.AddMinutes(minute - 1), start.AddMinutes(minute), false));
        var sample = CityResidentActivityObservation.ForTick(61, start.AddMinutes(59), start.AddHours(1), false);
        Assert.Equal(new CityResidentActivityObservation(61, start.AddHours(1)), sample);
        Assert.NotNull(CityResidentActivityObservation.ForTick(62, start, start.AddDays(1), false));
        Assert.Null(CityResidentActivityObservation.ForTick(63, start, start, false));
    }
}
