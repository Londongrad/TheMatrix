using Matrix.Identity.Infrastructure.Time;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Time;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsUtcDateTimeFromInjectedTimeProvider()
    {
        DateTimeOffset now = new(2050, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var clock = new SystemClock(CreateTimeProvider(now));

        Assert.Equal(now.UtcDateTime, clock.UtcNow);
        Assert.Equal(DateTimeKind.Utc, clock.UtcNow.Kind);
    }
}
