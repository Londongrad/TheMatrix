namespace Matrix.ApiGateway.Infrastructure.Caching
{
    internal static class CacheLoggingDefaults
    {
        // Порог, после которого считаем Redis/cache операцию "медленной"
        internal const int SlowOperationMs = 300;

        // период антишторма для slow-логов
        internal static readonly TimeSpan SlowPeriod = TimeSpan.FromSeconds(30);
        internal static readonly TimeSpan FailPeriod = TimeSpan.FromSeconds(15);
        internal static readonly TimeSpan InvalidPeriod = TimeSpan.FromMinutes(5);
    }
}
