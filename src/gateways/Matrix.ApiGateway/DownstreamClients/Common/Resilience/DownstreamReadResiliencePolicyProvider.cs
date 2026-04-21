using System.Collections.Concurrent;
using System.Net;
using Matrix.ApiGateway.Configurations.Options;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace Matrix.ApiGateway.DownstreamClients.Common.Resilience
{
    public sealed class DownstreamReadResiliencePolicyProvider
    {
        private static readonly IAsyncPolicy<HttpResponseMessage> NoOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();

        private readonly ConcurrentDictionary<string, IAsyncPolicy<HttpResponseMessage>> _retryPolicies = new();
        private readonly ConcurrentDictionary<string, IAsyncPolicy<HttpResponseMessage>> _circuitBreakerPolicies = new();
        private readonly DownstreamReadResilienceOptions _options;
        private readonly ILogger<DownstreamReadResiliencePolicyProvider> _logger;

        public DownstreamReadResiliencePolicyProvider(
            IOptions<DownstreamReadResilienceOptions> options,
            ILogger<DownstreamReadResiliencePolicyProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
            string serviceName,
            HttpRequestMessage request)
        {
            if (!ShouldApplyResilience(request) || _options.MaxRetryAttempts <= 0)
            {
                return NoOpPolicy;
            }

            return _retryPolicies.GetOrAdd(serviceName, CreateRetryPolicy);
        }

        public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
            string serviceName,
            HttpRequestMessage request)
        {
            if (!ShouldApplyResilience(request))
            {
                return NoOpPolicy;
            }

            return _circuitBreakerPolicies.GetOrAdd(serviceName, CreateCircuitBreakerPolicy);
        }

        private static PolicyBuilder<HttpResponseMessage> CreatePolicyBuilder()
        {
            return HttpPolicyExtensions
               .HandleTransientHttpError()
               .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests);
        }

        private bool ShouldApplyResilience(HttpRequestMessage request)
        {
            if (!_options.Enabled)
            {
                return false;
            }

            return request.Method == HttpMethod.Get
                || request.Method == HttpMethod.Head;
        }

        private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(string serviceName)
        {
            return CreatePolicyBuilder()
               .WaitAndRetryAsync(
                    retryCount: _options.MaxRetryAttempts,
                    sleepDurationProvider: attempt =>
                    {
                        double exponentialBackoff = Math.Pow(2, attempt - 1);
                        double delayMilliseconds = _options.BaseRetryDelayMilliseconds * exponentialBackoff;
                        return TimeSpan.FromMilliseconds(delayMilliseconds);
                    },
                    onRetry: (outcome, delay, attempt, _) =>
                    {
                        string failure = DescribeOutcome(outcome);
                        _logger.LogWarning(
                            "Retrying downstream read against {ServiceName}. Attempt {Attempt}/{MaxRetryAttempts} in {DelayMilliseconds} ms after {Failure}.",
                            serviceName,
                            attempt,
                            _options.MaxRetryAttempts,
                            delay.TotalMilliseconds,
                            failure);
                    });
        }

        private IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(string serviceName)
        {
            return CreatePolicyBuilder()
               .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: _options.CircuitBreakerConsecutiveFailureThreshold,
                    durationOfBreak: TimeSpan.FromSeconds(_options.CircuitBreakDurationSeconds));
        }

        private static string DescribeOutcome(DelegateResult<HttpResponseMessage> outcome)
        {
            if (outcome.Exception is BrokenCircuitException)
            {
                return "an open circuit";
            }

            if (outcome.Exception is not null)
            {
                return outcome.Exception.GetType().Name;
            }

            HttpResponseMessage? response = outcome.Result;
            return response is null
                ? "an unknown failure"
                : $"{(int)response.StatusCode} {response.StatusCode}";
        }
    }
}
