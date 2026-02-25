namespace Matrix.ApiGateway.Configurations.Options
{
    public sealed class ClassicCitySetupSessionOptions
    {
        public const string SectionName = "ClassicCitySetupSessions";

        public int CacheTtlHours { get; init; } = 168;

        public int MutationLockLeaseSeconds { get; init; } = 900;

        public int MutationLockAcquireTimeoutMilliseconds { get; init; } = 1500;

        public int MutationLockRetryDelayMilliseconds { get; init; } = 100;
    }
}
