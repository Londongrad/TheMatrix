using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.Common.Resilience;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class DownstreamReadResilienceServiceCollectionExtensions
    {
        public static IServiceCollection AddDownstreamReadResilience(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<DownstreamReadResilienceOptions>()
               .Bind(configuration.GetSection(DownstreamReadResilienceOptions.SectionName))
               .Validate(
                    validation: o => o.MaxRetryAttempts >= 0,
                    failureMessage: $"{DownstreamReadResilienceOptions.SectionName}:MaxRetryAttempts must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.BaseRetryDelayMilliseconds > 0,
                    failureMessage: $"{DownstreamReadResilienceOptions.SectionName}:BaseRetryDelayMilliseconds must be greater than 0.")
               .Validate(
                    validation: o => o.CircuitBreakerConsecutiveFailureThreshold > 0,
                    failureMessage: $"{DownstreamReadResilienceOptions.SectionName}:CircuitBreakerConsecutiveFailureThreshold must be greater than 0.")
               .Validate(
                    validation: o => o.CircuitBreakDurationSeconds > 0,
                    failureMessage: $"{DownstreamReadResilienceOptions.SectionName}:CircuitBreakDurationSeconds must be greater than 0.")
               .ValidateOnStart();

            services.TryAddSingleton<DownstreamReadResiliencePolicyProvider>();

            return services;
        }

        public static IHttpClientBuilder AddDownstreamReadResilience(
            this IHttpClientBuilder builder,
            string serviceName)
        {
            return builder
               .AddPolicyHandler((sp, request) =>
               {
                   DownstreamReadResiliencePolicyProvider provider = sp.GetRequiredService<DownstreamReadResiliencePolicyProvider>();
                   return provider.GetRetryPolicy(serviceName, request);
               })
               .AddPolicyHandler((sp, request) =>
               {
                   DownstreamReadResiliencePolicyProvider provider = sp.GetRequiredService<DownstreamReadResiliencePolicyProvider>();
                   return provider.GetCircuitBreakerPolicy(serviceName, request);
               });
        }
    }
}
