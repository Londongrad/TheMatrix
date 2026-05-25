using System.Net;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Api.Configurations;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.StartupTestSupport;

namespace Matrix.Identity.Api.Tests.Configurations
{
    public sealed class ServicesConfigurationTests
    {
        [Fact]
        public void
            ConfigureApplicationServices_WhenIdentityDbConnectionStringIsMissing_ThrowsInvalidOperationException()
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:IdentityDb"] = "",
                        ["ExternalJwt:Issuer"] = "https://identity.test",
                        ["ExternalJwt:Audience"] = "matrix-clients",
                        ["ExternalJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                        ["IdentityInternal:ApiKey"] = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&"
                    })
               .Build();
            WebApplicationBuilder builder = CreateBuilder(configuration);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                builder.ConfigureApplicationServices);

            Assert.Contains(
                expectedSubstring: "Connection string 'IdentityDb' is not configured",
                actualString: exception.Message);
        }

        [Fact]
        public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersSecurityAndApiServices()
        {
            WebApplicationBuilder builder = CreateBuilder(BuildValidApiConfiguration());

            builder.ConfigureApplicationServices();

            using ServiceProvider provider = builder.Services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUserContext>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEffectivePermissionsService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAvatarStorage>());

            AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>()
               .Value;
            ExternalJwtOptions externalJwt = provider.GetRequiredService<IOptions<ExternalJwtOptions>>()
               .Value;
            IdentityInternalOptions internalOptions = provider.GetRequiredService<IOptions<IdentityInternalOptions>>()
               .Value;
            TrustedForwardedHeadersOptions trustedForwarding =
                provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>()
                   .Value;
            ForwardedHeadersOptions forwardedHeaders =
                provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>()
                   .Value;

            Assert.Equal(
                expected: JwtBearerDefaults.AuthenticationScheme,
                actual: authentication.DefaultAuthenticateScheme);
            Assert.Equal(
                expected: JwtBearerDefaults.AuthenticationScheme,
                actual: authentication.DefaultChallengeScheme);
            Assert.Equal(
                expected: "https://identity.test",
                actual: externalJwt.Issuer);
            Assert.Equal(
                expected: "matrix-clients",
                actual: externalJwt.Audience);
            Assert.Equal(
                expected: "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&",
                actual: internalOptions.ApiKey);
            Assert.True(trustedForwarding.Enabled);
            Assert.Equal(
                expected: 2,
                actual: forwardedHeaders.ForwardLimit);
            Assert.Contains(
                collection: forwardedHeaders.KnownProxies,
                filter: proxy => proxy.Equals(IPAddress.Loopback));
            Assert.Contains(
                collection: forwardedHeaders.KnownProxies,
                filter: proxy => proxy.Equals(IPAddress.IPv6Loopback));
        }
    }
}
