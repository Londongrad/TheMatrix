using Matrix.Identity.Application.Abstractions.Services;

namespace Matrix.Identity.Infrastructure.Time
{
    public sealed class SystemClock(TimeProvider timeProvider) : IClock
    {
        public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
    }
}
