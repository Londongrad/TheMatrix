using System.Net;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.Common.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Xunit;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Common;

public sealed class DownstreamReadResiliencePolicyProviderTests
{
    [Fact]
    public async Task GetRetryPolicy_WhenRequestIsGet_RetriesTransientFailures()
    {
        var provider = CreateProvider(maxRetryAttempts: 2, baseRetryDelayMilliseconds: 1);
        HttpRequestMessage request = new(HttpMethod.Get, "https://population.test/cities");
        var policy = provider.GetRetryPolicy("Population", request);
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(
                attempts < 3 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
        }, CancellationToken.None);

        Assert.Equal(3, attempts);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Same(policy, provider.GetRetryPolicy("Population", new HttpRequestMessage(HttpMethod.Get, "https://population.test/other")));
    }

    [Fact]
    public async Task GetRetryPolicy_WhenRequestIsNotRead_DoesNotRetry()
    {
        var provider = CreateProvider(maxRetryAttempts: 3, baseRetryDelayMilliseconds: 1);
        HttpRequestMessage request = new(HttpMethod.Post, "https://population.test/cities");
        var policy = provider.GetRetryPolicy("Population", request);
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }, CancellationToken.None);

        Assert.Equal(1, attempts);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetCircuitBreakerPolicy_WhenRequestIsGet_OpensCircuitAfterThreshold()
    {
        var provider = CreateProvider(
            maxRetryAttempts: 0,
            baseRetryDelayMilliseconds: 1,
            circuitBreakerConsecutiveFailureThreshold: 1,
            circuitBreakDurationSeconds: 60);
        HttpRequestMessage request = new(HttpMethod.Get, "https://economy.test/summary");
        var policy = provider.GetCircuitBreakerPolicy("Economy", request);

        using HttpResponseMessage first = await policy.ExecuteAsync(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, first.StatusCode);

        Exception exception = await Assert.ThrowsAnyAsync<BrokenCircuitException>(() =>
            policy.ExecuteAsync(
                _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                CancellationToken.None));

        Assert.IsAssignableFrom<BrokenCircuitException>(exception);
    }

    [Fact]
    public async Task GetCircuitBreakerPolicy_WhenResilienceIsDisabled_ReturnsNoOpPolicy()
    {
        var provider = CreateProvider(enabled: false);
        HttpRequestMessage request = new(HttpMethod.Get, "https://resources.test/stockpiles");
        var policy = provider.GetCircuitBreakerPolicy("Resources", request);
        int attempts = 0;

        HttpResponseMessage response = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }, CancellationToken.None);

        Assert.Equal(1, attempts);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Same(policy, provider.GetCircuitBreakerPolicy("Resources", new HttpRequestMessage(HttpMethod.Post, "https://resources.test/stockpiles")));
    }

    private static DownstreamReadResiliencePolicyProvider CreateProvider(
        bool enabled = true,
        int maxRetryAttempts = 2,
        int baseRetryDelayMilliseconds = 200,
        int circuitBreakerConsecutiveFailureThreshold = 5,
        int circuitBreakDurationSeconds = 30)
    {
        return new DownstreamReadResiliencePolicyProvider(
            Options.Create(new DownstreamReadResilienceOptions
            {
                Enabled = enabled,
                MaxRetryAttempts = maxRetryAttempts,
                BaseRetryDelayMilliseconds = baseRetryDelayMilliseconds,
                CircuitBreakerConsecutiveFailureThreshold = circuitBreakerConsecutiveFailureThreshold,
                CircuitBreakDurationSeconds = circuitBreakDurationSeconds
            }),
            NullLogger<DownstreamReadResiliencePolicyProvider>.Instance);
    }
}
