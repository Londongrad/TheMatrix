using System.Net;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Api.Configurations;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.StartupTestSupport;

namespace Matrix.Identity.Api.Tests.Configurations;

public sealed class ServicesConfigurationTests
{
    [Fact]
    public void ConfigureApplicationServices_WhenIdentityDbConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
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

        Assert.Contains("Connection string 'IdentityDb' is not configured", exception.Message);
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

        AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        ExternalJwtOptions externalJwt = provider.GetRequiredService<IOptions<ExternalJwtOptions>>().Value;
        IdentityInternalOptions internalOptions = provider.GetRequiredService<IOptions<IdentityInternalOptions>>().Value;
        TrustedForwardedHeadersOptions trustedForwarding =
            provider.GetRequiredService<IOptions<TrustedForwardedHeadersOptions>>().Value;
        ForwardedHeadersOptions forwardedHeaders =
            provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authentication.DefaultChallengeScheme);
        Assert.Equal("https://identity.test", externalJwt.Issuer);
        Assert.Equal("matrix-clients", externalJwt.Audience);
        Assert.Equal("A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&", internalOptions.ApiKey);
        Assert.True(trustedForwarding.Enabled);
        Assert.Equal(2, forwardedHeaders.ForwardLimit);
        Assert.Contains(forwardedHeaders.KnownProxies, proxy => proxy.Equals(IPAddress.Loopback));
        Assert.Contains(forwardedHeaders.KnownProxies, proxy => proxy.Equals(IPAddress.IPv6Loopback));
    }
}
