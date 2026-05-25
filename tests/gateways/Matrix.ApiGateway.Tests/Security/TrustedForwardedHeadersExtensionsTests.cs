using System.Net;
using Matrix.BuildingBlocks.Api.Forwarding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Security
{
    public sealed class TrustedForwardedHeadersExtensionsTests
    {
        [Fact]
        public void AddTrustedForwardedHeaders_WhenConfigurationIsValid_ConfiguresForwardedHeaderOptions()
        {
            IConfiguration configuration = BuildConfiguration(
                new Dictionary<string, string?>
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
                provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>()
                   .Value;
            ForwardedHeadersOptions forwardedOptions =
                provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>()
                   .Value;

            Assert.True(trustedOptions.Enabled);
            Assert.True(trustedOptions.TrustLoopback);
            Assert.Equal(
                expected: 2,
                actual: trustedOptions.ForwardLimit);
            Assert.Equal(
                expected: ForwardedHeaders.XForwardedFor |
                          ForwardedHeaders.XForwardedProto |
                          ForwardedHeaders.XForwardedHost,
                actual: forwardedOptions.ForwardedHeaders);
            Assert.Equal(
                expected: 2,
                actual: forwardedOptions.ForwardLimit);
            Assert.Contains(
                collection: forwardedOptions.KnownProxies,
                filter: proxy => proxy.Equals(IPAddress.Loopback));
            Assert.Contains(
                collection: forwardedOptions.KnownProxies,
                filter: proxy => proxy.Equals(IPAddress.IPv6Loopback));
            Assert.Contains(
                collection: forwardedOptions.KnownProxies,
                filter: proxy => proxy.Equals(IPAddress.Parse("10.0.0.15")));
            Assert.Single(forwardedOptions.KnownNetworks);
        }

        [Fact]
        public void AddTrustedForwardedHeaders_WhenForwardLimitIsInvalid_ThrowsOptionsValidationException()
        {
            IConfiguration configuration = BuildConfiguration(
                new Dictionary<string, string?>
                {
                    ["TrustedForwardedHeaders:Enabled"] = "true",
                    ["TrustedForwardedHeaders:TrustLoopback"] = "true",
                    ["TrustedForwardedHeaders:ForwardLimit"] = "0"
                });
            ServiceCollection services = new();

            services.AddTrustedForwardedHeaders(configuration);

            using ServiceProvider provider = services.BuildServiceProvider();

            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => provider
               .GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>()
               .Value);

            Assert.Contains(
                expectedSubstring: "ForwardLimit",
                actualString: string.Join(
                    separator: " | ",
                    values: exception.Failures));
        }

        [Fact]
        public void AddTrustedForwardedHeaders_WhenNoTrustedTargetsConfigured_ThrowsOptionsValidationException()
        {
            IConfiguration configuration = BuildConfiguration(
                new Dictionary<string, string?>
                {
                    ["TrustedForwardedHeaders:Enabled"] = "true",
                    ["TrustedForwardedHeaders:TrustLoopback"] = "false"
                });
            ServiceCollection services = new();

            services.AddTrustedForwardedHeaders(configuration);

            using ServiceProvider provider = services.BuildServiceProvider();

            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => provider
               .GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>()
               .Value);

            Assert.Contains(
                expectedSubstring: "trusted proxy/network",
                actualString: string.Join(
                    separator: " | ",
                    values: exception.Failures),
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AddTrustedForwardedHeaders_WhenKnownProxyIsInvalid_ThrowsOptionsValidationException()
        {
            IConfiguration configuration = BuildConfiguration(
                new Dictionary<string, string?>
                {
                    ["TrustedForwardedHeaders:Enabled"] = "true",
                    ["TrustedForwardedHeaders:TrustLoopback"] = "false",
                    ["TrustedForwardedHeaders:KnownProxies:0"] = "not-an-ip"
                });
            ServiceCollection services = new();

            services.AddTrustedForwardedHeaders(configuration);

            using ServiceProvider provider = services.BuildServiceProvider();

            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => provider
               .GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>()
               .Value);

            Assert.Contains(
                expectedSubstring: "KnownProxies",
                actualString: string.Join(
                    separator: " | ",
                    values: exception.Failures));
        }

        [Fact]
        public void GetNormalizedClientIpAddress_WhenContextUsesIpv4MappedIpv6_ReturnsIpv4Address()
        {
            DefaultHttpContext httpContext = new();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.168.10.22");

            string? result = httpContext.GetNormalizedClientIpAddress();

            Assert.Equal(
                expected: "192.168.10.22",
                actual: result);
        }

        [Fact]
        public void NormalizeClientIpAddress_WhenValueIsInvalidOrMapped_NormalizesCorrectly()
        {
            Assert.Null(TrustedForwardedHeadersExtensions.NormalizeClientIpAddress("not-an-ip"));
            Assert.Equal(
                expected: "172.16.1.8",
                actual: TrustedForwardedHeadersExtensions.NormalizeClientIpAddress("::ffff:172.16.1.8"));
            Assert.Equal(
                expected: "2001:db8::1",
                actual: TrustedForwardedHeadersExtensions.NormalizeClientIpAddress("2001:db8::1"));
        }

        private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }
    }
}
