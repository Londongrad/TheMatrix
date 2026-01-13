namespace Matrix.ApiGateway.Infrastructure.Caching
{
    internal static class RedisCacheLogKeys
    {
        internal const string PvCacheTtlInvalid = "pv.cache.ttl.invalid";
        internal const string PvRedisReadSlow = "pv.redis.read.slow";
        internal const string PvRedisReadInvalid = "pv.redis.read.invalid";
        internal const string PvRedisReadFail = "pv.redis.read.fail";
        internal const string PvRedisWriteSlow = "pv.redis.write.slow";
        internal const string PvRedisWriteFail = "pv.redis.write.fail";

        internal const string AcCacheTtlInvalid = "ac.cache.ttl.invalid";
        internal const string AcRedisReadSlow = "ac.redis.read.slow";
        internal const string AcRedisReadInvalid = "ac.redis.read.invalid";
        internal const string AcRedisReadFail = "ac.redis.read.fail";
        internal const string AcRedisWriteSlow = "ac.redis.write.slow";
        internal const string AcRedisWriteFail = "ac.redis.write.fail";
    }
}
