using System.Collections.Concurrent;
using System.Diagnostics;

namespace Matrix.ApiGateway.Infrastructure.Logging
{
    internal static class LogRateLimiter
    {
        private static readonly ConcurrentDictionary<string, long> LastLogAt = new();

        public static bool ShouldLog(
            string key,
            TimeSpan period)
        {
            long now = Stopwatch.GetTimestamp();
            long periodTicks = (long)(period.TotalSeconds * Stopwatch.Frequency);

            while (true)
            {
                long last = LastLogAt.GetOrAdd(
                    key: key,
                    value: 0);

                if (now - last < periodTicks)
                    return false;

                if (LastLogAt.TryUpdate(
                        key: key,
                        newValue: now,
                        comparisonValue: last))
                    return true;

                // someone updated concurrently; retry
            }
        }
    }
}
