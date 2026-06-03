namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public static class ClassicCitySetupSessionServiceCollectionExtensions
    {
        public static IServiceCollection AddClassicCitySetupSessionServices(this IServiceCollection services)
        {
            services
               .AddScoped<IClassicCitySetupSessionStore, RedisClassicCitySetupSessionStore>()
               .AddScoped<IClassicCitySetupSessionService, ClassicCitySetupSessionService>()
               .AddHostedService<ClassicCitySetupSessionRecoveryHostedService>();

            return services;
        }

        public static IServiceCollection AddClassicCitySetupSessionOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<ClassicCitySetupSessionOptions>()
               .Bind(configuration.GetSection(ClassicCitySetupSessionOptions.SectionName))
               .Validate(
                    validation: o => o.CacheTtlHours > 0,
                    failureMessage: "ClassicCitySetupSessions:CacheTtlHours must be greater than 0.")
               .Validate(
                    validation: o => o.DraftTtlMinutes > 0,
                    failureMessage: "ClassicCitySetupSessions:DraftTtlMinutes must be greater than 0.")
               .Validate(
                    validation: o => o.RecentDraftReuseWindowSeconds > 0,
                    failureMessage: "ClassicCitySetupSessions:RecentDraftReuseWindowSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockLeaseSeconds > 0,
                    failureMessage: "ClassicCitySetupSessions:MutationLockLeaseSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockAcquireTimeoutMilliseconds > 0,
                    failureMessage:
                    "ClassicCitySetupSessions:MutationLockAcquireTimeoutMilliseconds must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockRetryDelayMilliseconds > 0,
                    failureMessage:
                    "ClassicCitySetupSessions:MutationLockRetryDelayMilliseconds must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockAcquireTimeoutMilliseconds >= o.MutationLockRetryDelayMilliseconds,
                    failureMessage:
                    "ClassicCitySetupSessions:MutationLockAcquireTimeoutMilliseconds must be greater than or equal to MutationLockRetryDelayMilliseconds.")
               .Validate(
                    validation: o => o.ReconciliationIntervalSeconds > 0,
                    failureMessage: "ClassicCitySetupSessions:ReconciliationIntervalSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.LaunchQueueRecoveryDelaySeconds > 0,
                    failureMessage: "ClassicCitySetupSessions:LaunchQueueRecoveryDelaySeconds must be greater than 0.")
               .ValidateOnStart();

            return services;
        }
    }
}
