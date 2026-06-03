namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionOptions
    {
        public const string SectionName = "ClassicCitySetupSessions";

        public int CacheTtlHours { get; init; } = 168;

        public int DraftTtlMinutes { get; init; } = 60;

        public int RecentDraftReuseWindowSeconds { get; init; } = 30;

        public int MutationLockLeaseSeconds { get; init; } = 900;

        public int MutationLockAcquireTimeoutMilliseconds { get; init; } = 1500;

        public int MutationLockRetryDelayMilliseconds { get; init; } = 100;

        public bool ReconciliationEnabled { get; init; } = true;

        public int ReconciliationIntervalSeconds { get; init; } = 15;

        public int LaunchQueueRecoveryDelaySeconds { get; init; } = 20;
    }
}
