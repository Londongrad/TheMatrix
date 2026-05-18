using System.Net;
using Matrix.BuildingBlocks.Api.Forwarding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Security;

public sealed class TrustedForwardedHeadersExtensionsTests
{
    [Fact]
    public void AddTrustedForwardedHeaders_WhenConfigurationIsValid_ConfiguresForwardedHeaderOptions()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["TrustedForwardedHeaders:Enabled"] = "true",
            ["TrustedForwardedHeaders:TrustLoopback"] = "true",
            ["TrustedForwardedHeaders:ForwardLimit"] = "2",
            ["TrustedForwardedHeaders:KnownProxies:0"] = "10.0.0.15",
            ["TrustedForwardedHeaders:KnownNetworks:0"] = "10.20.0.0/16"
        });
        ServiceCollection services = new();

        services.AddTrustedForwardedHeaders(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        TrustedForwardedHeadersOptions trustedOptions =
            provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>().Value;
        ForwardedHeadersOptions forwardedOptions =
            provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.True(trustedOptions.Enabled);
        Assert.True(trustedOptions.TrustLoopback);
        Assert.Equal(2, trustedOptions.ForwardLimit);
        Assert.Equal(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            forwardedOptions.ForwardedHeaders);
        Assert.Equal(2, forwardedOptions.ForwardLimit);
        Assert.Contains(forwardedOptions.KnownProxies, proxy => proxy.Equals(IPAddress.Loopback));
        Assert.Contains(forwardedOptions.KnownProxies, proxy => proxy.Equals(IPAddress.IPv6Loopback));
        Assert.Contains(forwardedOptions.KnownProxies, proxy => proxy.Equals(IPAddress.Parse("10.0.0.15")));
        Assert.Single(forwardedOptions.KnownNetworks);
    }

    [Fact]
    public void AddTrustedForwardedHeaders_WhenForwardLimitIsInvalid_ThrowsOptionsValidationException()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["TrustedForwardedHeaders:Enabled"] = "true",
            ["TrustedForwardedHeaders:TrustLoopback"] = "true",
            ["TrustedForwardedHeaders:ForwardLimit"] = "0"
        });
        ServiceCollection services = new();

        services.AddTrustedForwardedHeaders(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>().Value);

        Assert.Contains("ForwardLimit", string.Join(" | ", exception.Failures));
    }

    [Fact]
    public void AddTrustedForwardedHeaders_WhenNoTrustedTargetsConfigured_ThrowsOptionsValidationException()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["TrustedForwardedHeaders:Enabled"] = "true",
            ["TrustedForwardedHeaders:TrustLoopback"] = "false"
        });
        ServiceCollection services = new();

        services.AddTrustedForwardedHeaders(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>().Value);

        Assert.Contains("trusted proxy/network", string.Join(" | ", exception.Failures), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTrustedForwardedHeaders_WhenKnownProxyIsInvalid_ThrowsOptionsValidationException()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["TrustedForwardedHeaders:Enabled"] = "true",
            ["TrustedForwardedHeaders:TrustLoopback"] = "false",
            ["TrustedForwardedHeaders:KnownProxies:0"] = "not-an-ip"
        });
        ServiceCollection services = new();

        services.AddTrustedForwardedHeaders(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>().Value);

        Assert.Contains("KnownProxies", string.Join(" | ", exception.Failures));
    }

    [Fact]
    public void GetNormalizedClientIpAddress_WhenContextUsesIpv4MappedIpv6_ReturnsIpv4Address()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.168.10.22");

        string? result = httpContext.GetNormalizedClientIpAddress();

        Assert.Equal("192.168.10.22", result);
    }

    [Fact]
    public void NormalizeClientIpAddress_WhenValueIsInvalidOrMapped_NormalizesCorrectly()
    {
        Assert.Null(TrustedForwardedHeadersExtensions.NormalizeClientIpAddress("not-an-ip"));
        Assert.Equal("172.16.1.8", TrustedForwardedHeadersExtensions.NormalizeClientIpAddress("::ffff:172.16.1.8"));
        Assert.Equal("2001:db8::1", TrustedForwardedHeadersExtensions.NormalizeClientIpAddress("2001:db8::1"));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
