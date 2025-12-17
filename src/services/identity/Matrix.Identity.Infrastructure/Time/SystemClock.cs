using Matrix.Identity.Application.Abstractions.Services;

namespace Matrix.Identity.Infrastructure.Time
{
    public class SystemClock : IClock
    {
        public DateTime UtcNow { get; }
    }
}
