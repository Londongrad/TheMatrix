using System.Net;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.Common.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Xunit;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Common
{
    public sealed class DownstreamReadResiliencePolicyProviderTests
    {
        [Fact]
        public async Task GetRetryPolicy_WhenRequestIsGet_RetriesTransientFailures()
        {
            DownstreamReadResiliencePolicyProvider provider = CreateProvider(
                maxRetryAttempts: 2,
                baseRetryDelayMilliseconds: 1);
            HttpRequestMessage request = new(
                method: HttpMethod.Get,
                requestUri: "https://population.test/cities");
            IAsyncPolicy<HttpResponseMessage> policy = provider.GetRetryPolicy(
                serviceName: "Population",
                request: request);
            int attempts = 0;

            HttpResponseMessage response = await policy.ExecuteAsync(
                action: _ =>
                {
                    attempts++;
                    return Task.FromResult(
                        new HttpResponseMessage(
                            attempts < 3
                                ? HttpStatusCode.InternalServerError
                                : HttpStatusCode.OK));
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 3,
                actual: attempts);
            Assert.Equal(
                expected: HttpStatusCode.OK,
                actual: response.StatusCode);
            Assert.Same(
                expected: policy,
                actual: provider.GetRetryPolicy(
                    serviceName: "Population",
                    request: new HttpRequestMessage(
                        method: HttpMethod.Get,
                        requestUri: "https://population.test/other")));
        }

        [Fact]
        public async Task GetRetryPolicy_WhenRequestIsNotRead_DoesNotRetry()
        {
            DownstreamReadResiliencePolicyProvider provider = CreateProvider(
                maxRetryAttempts: 3,
                baseRetryDelayMilliseconds: 1);
            HttpRequestMessage request = new(
                method: HttpMethod.Post,
                requestUri: "https://population.test/cities");
            IAsyncPolicy<HttpResponseMessage> policy = provider.GetRetryPolicy(
                serviceName: "Population",
                request: request);
            int attempts = 0;

            HttpResponseMessage response = await policy.ExecuteAsync(
                action: _ =>
                {
                    attempts++;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: attempts);
            Assert.Equal(
                expected: HttpStatusCode.InternalServerError,
                actual: response.StatusCode);
        }

        [Fact]
        public async Task GetCircuitBreakerPolicy_WhenRequestIsGet_OpensCircuitAfterThreshold()
        {
            DownstreamReadResiliencePolicyProvider provider = CreateProvider(
                maxRetryAttempts: 0,
                baseRetryDelayMilliseconds: 1,
                circuitBreakerConsecutiveFailureThreshold: 1,
                circuitBreakDurationSeconds: 60);
            HttpRequestMessage request = new(
                method: HttpMethod.Get,
                requestUri: "https://economy.test/summary");
            IAsyncPolicy<HttpResponseMessage> policy = provider.GetCircuitBreakerPolicy(
                serviceName: "Economy",
                request: request);

            using HttpResponseMessage first = await policy.ExecuteAsync(
                action: _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: HttpStatusCode.ServiceUnavailable,
                actual: first.StatusCode);

            Exception exception = await Assert.ThrowsAnyAsync<BrokenCircuitException>(() =>
                policy.ExecuteAsync(
                    action: _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                    cancellationToken: CancellationToken.None));

            Assert.IsAssignableFrom<BrokenCircuitException>(exception);
        }

        [Fact]
        public async Task GetCircuitBreakerPolicy_WhenResilienceIsDisabled_ReturnsNoOpPolicy()
        {
            DownstreamReadResiliencePolicyProvider provider = CreateProvider(enabled: false);
            HttpRequestMessage request = new(
                method: HttpMethod.Get,
                requestUri: "https://resources.test/stockpiles");
            IAsyncPolicy<HttpResponseMessage> policy = provider.GetCircuitBreakerPolicy(
                serviceName: "Resources",
                request: request);
            int attempts = 0;

            HttpResponseMessage response = await policy.ExecuteAsync(
                action: _ =>
                {
                    attempts++;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: attempts);
            Assert.Equal(
                expected: HttpStatusCode.InternalServerError,
                actual: response.StatusCode);
            Assert.Same(
                expected: policy,
                actual: provider.GetCircuitBreakerPolicy(
                    serviceName: "Resources",
                    request: new HttpRequestMessage(
                        method: HttpMethod.Post,
                        requestUri: "https://resources.test/stockpiles")));
        }

        private static DownstreamReadResiliencePolicyProvider CreateProvider(
            bool enabled = true,
            int maxRetryAttempts = 2,
            int baseRetryDelayMilliseconds = 200,
            int circuitBreakerConsecutiveFailureThreshold = 5,
            int circuitBreakDurationSeconds = 30)
        {
            return new DownstreamReadResiliencePolicyProvider(
                options: Options.Create(
                    new DownstreamReadResilienceOptions
                    {
                        Enabled = enabled,
                        MaxRetryAttempts = maxRetryAttempts,
                        BaseRetryDelayMilliseconds = baseRetryDelayMilliseconds,
                        CircuitBreakerConsecutiveFailureThreshold = circuitBreakerConsecutiveFailureThreshold,
                        CircuitBreakDurationSeconds = circuitBreakDurationSeconds
                    }),
                logger: NullLogger<DownstreamReadResiliencePolicyProvider>.Instance);
        }
    }
}
