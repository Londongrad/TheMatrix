using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AspNetCoreIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Matrix.BuildingBlocks.Api.Forwarding
{
    public static class TrustedForwardedHeadersExtensions
    {
        public static IServiceCollection AddTrustedForwardedHeaders(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = TrustedForwardedHeadersOptions.SectionName)
        {
            services.AddOptions<TrustedForwardedHeadersOptions>()
               .Bind(configuration.GetSection(sectionName))
               .Validate(
                    validation: options => !options.Enabled ||
                                           !options.ForwardLimit.HasValue ||
                                           options.ForwardLimit.Value > 0,
                    failureMessage: $"{sectionName}:ForwardLimit must be greater than 0 when specified.")
               .Validate(
                    validation: options => !options.Enabled ||
                                           options.TrustLoopback ||
                                           options.KnownProxies.Length > 0 ||
                                           options.KnownNetworks.Length > 0,
                    failureMessage:
                    $"{sectionName}: configure at least one trusted proxy/network or enable loopback trust.")
               .Validate(
                    validation: options => !options.Enabled ||
                                           options.KnownProxies.All(IsValidIpAddress),
                    failureMessage: $"{sectionName}:KnownProxies must contain valid IP addresses.")
               .Validate(
                    validation: options => !options.Enabled ||
                                           options.KnownNetworks.All(IsValidNetwork),
                    failureMessage: $"{sectionName}:KnownNetworks must contain valid CIDR ranges.")
               .ValidateOnStart();

            services.AddOptions<ForwardedHeadersOptions>()
               .Configure<IOptions<TrustedForwardedHeadersOptions>>((forwardedHeaders, trustedOptions) =>
                {
                    TrustedForwardedHeadersOptions options = trustedOptions.Value;
                    if (!options.Enabled)
                        return;

                    forwardedHeaders.ForwardedHeaders =
                        ForwardedHeaders.XForwardedFor |
                        ForwardedHeaders.XForwardedProto |
                        ForwardedHeaders.XForwardedHost;

                    forwardedHeaders.ForwardLimit = options.ForwardLimit;
                    forwardedHeaders.KnownProxies.Clear();
                    forwardedHeaders.KnownNetworks.Clear();

                    if (options.TrustLoopback)
                    {
                        forwardedHeaders.KnownProxies.Add(IPAddress.Loopback);
                        forwardedHeaders.KnownProxies.Add(IPAddress.IPv6Loopback);
                    }

                    foreach (IPAddress proxy in options.KnownProxies
                                .Where(proxy => !string.IsNullOrWhiteSpace(proxy))
                                .Distinct(StringComparer.Ordinal)
                                .Select(IPAddress.Parse))
                        forwardedHeaders.KnownProxies.Add(proxy);

                    foreach (AspNetCoreIPNetwork network in options.KnownNetworks
                                .Where(network => !string.IsNullOrWhiteSpace(network))
                                .Distinct(StringComparer.Ordinal)
                                .Select(network => AspNetCoreIPNetwork.Parse(network.AsSpan())))
                        forwardedHeaders.KnownNetworks.Add(network);
                });

            return services;
        }

        public static IApplicationBuilder UseTrustedForwardedHeaders(this IApplicationBuilder app)
        {
            TrustedForwardedHeadersOptions options = app.ApplicationServices
               .GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>()
               .Value;

            if (options.Enabled)
                app.UseForwardedHeaders();

            return app;
        }

        public static string? GetNormalizedClientIpAddress(this HttpContext context)
        {
            IPAddress? remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp is null)
                return null;

            if (remoteIp.IsIPv4MappedToIPv6)
                remoteIp = remoteIp.MapToIPv4();

            return remoteIp.ToString();
        }

        public static string? NormalizeClientIpAddress(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !IPAddress.TryParse(
                    ipString: value,
                    address: out IPAddress? ipAddress))
                return null;

            if (ipAddress.IsIPv4MappedToIPv6)
                ipAddress = ipAddress.MapToIPv4();

            return ipAddress.ToString();
        }

        private static bool IsValidIpAddress(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   IPAddress.TryParse(
                       ipString: value,
                       address: out _);
        }

        private static bool IsValidNetwork(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   AspNetCoreIPNetwork.TryParse(
                       value.AsSpan(),
                       out _);
        }
    }
}
