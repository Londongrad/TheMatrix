using System.Security.Claims;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.BuildingBlocks.Api.Tests.TestSupport.BuildingBlocksApiTestSupport;

namespace Matrix.BuildingBlocks.Api.Tests.Authorization;

public sealed class JwtAuthenticationExtensionsTests
{
    [Fact]
    public async Task AddJwtBearerAuthentication_WhenConfigurationIsValid_ConfiguresAuthenticationAndJwtBearerOptions()
    {
        ServiceCollection services = new();
        services.AddJwtBearerAuthentication<ExternalJwtOptions>(
            configuration: BuildConfiguration(new Dictionary<string, string?>
            {
                ["ExternalJwt:Issuer"] = "https://issuer.test",
                ["ExternalJwt:Audience"] = "matrix-clients",
                ["ExternalJwt:SigningKey"] = "0123456789abcdef0123456789abcdef"
            }),
            sectionName: ExternalJwtOptions.SectionName,
            requireHttpsMetadata: true,
            saveToken: true);

        using ServiceProvider provider = services.BuildServiceProvider();
        IAuthenticationSchemeProvider schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        ExternalJwtOptions jwtOptions = provider.GetRequiredService<IOptions<ExternalJwtOptions>>().Value;
        JwtBearerOptions bearerOptions =
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        AuthenticationScheme? defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();

        Assert.NotNull(defaultScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, defaultScheme!.Name);
        Assert.Equal("https://issuer.test", jwtOptions.Issuer);
        Assert.Equal("matrix-clients", bearerOptions.TokenValidationParameters.ValidAudience);
        Assert.True(bearerOptions.RequireHttpsMetadata);
        Assert.True(bearerOptions.SaveToken);
        Assert.NotNull(bearerOptions.TokenValidationParameters.IssuerSigningKey);
    }

    [Fact]
    public void AddJwtBearerAuthentication_WhenIssuerIsMissing_ThrowsOptionsValidationExceptionOnResolve()
    {
        ServiceCollection services = new();
        services.AddJwtBearerAuthentication<ExternalJwtOptions>(
            configuration: BuildConfiguration(new Dictionary<string, string?>
            {
                ["ExternalJwt:Audience"] = "matrix-clients",
                ["ExternalJwt:SigningKey"] = "0123456789abcdef0123456789abcdef"
            }),
            sectionName: ExternalJwtOptions.SectionName);

        using ServiceProvider provider = services.BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ExternalJwtOptions>>().Value);

        Assert.Contains("Issuer is required", string.Join(" | ", exception.Failures));
    }

    [Fact]
    public void AddInternalJwtAuthentication_WhenConfigured_RegistersCompositeSchemeAndResolvesForwardingByTokenKind()
    {
        ServiceCollection services = new();
        services.AddInternalJwtAuthentication(
            configuration: BuildConfiguration(new Dictionary<string, string?>
            {
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "internal-clients",
                ["InternalUserContextJwt:CurrentKeyId"] = "current-user",
                ["InternalUserContextJwt:Keys:current-user"] = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "internal-services",
                ["InternalServiceJwt:CurrentKeyId"] = "current-service",
                ["InternalServiceJwt:Keys:current-service"] = "Z9y8X7w6V5u4T3s2R1q0P)o(I*u&Y^t%R$"
            }),
            requireHttpsMetadata: true,
            saveToken: true);

        using ServiceProvider provider = services.BuildServiceProvider();
        AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        PolicySchemeOptions compositeOptions =
            provider.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get(JwtAuthenticationExtensions.InternalCompositeJwtScheme);
        JwtBearerOptions userContextOptions =
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtAuthenticationExtensions.InternalUserContextJwtScheme);
        JwtBearerOptions serviceOptions =
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtAuthenticationExtensions.InternalServiceJwtScheme);

        DefaultHttpContext serviceContext = new();
        serviceContext.Request.Headers.Authorization = $"Bearer {CreateUnsignedJwt(new Claim(JwtClaimNames.InternalTokenKind, InternalJwtTokenKinds.Service))}";

        DefaultHttpContext userContext = new();
        userContext.Request.Headers.Authorization = $"Bearer {CreateUnsignedJwt(new Claim(JwtClaimNames.InternalTokenKind, InternalJwtTokenKinds.UserContext))}";

        DefaultHttpContext malformedContext = new();
        malformedContext.Request.Headers.Authorization = "Bearer not-a-jwt";

        Assert.Equal(JwtAuthenticationExtensions.InternalCompositeJwtScheme, authentication.DefaultAuthenticateScheme);
        Assert.Equal(JwtAuthenticationExtensions.InternalServiceJwtScheme, compositeOptions.ForwardDefaultSelector!(serviceContext));
        Assert.Equal(JwtAuthenticationExtensions.InternalUserContextJwtScheme, compositeOptions.ForwardDefaultSelector!(userContext));
        Assert.Equal(JwtAuthenticationExtensions.InternalUserContextJwtScheme, compositeOptions.ForwardDefaultSelector!(malformedContext));
        Assert.True(userContextOptions.RequireHttpsMetadata);
        Assert.True(serviceOptions.SaveToken);
        Assert.NotNull(userContextOptions.TokenValidationParameters.IssuerSigningKeyResolver);
        Assert.NotNull(serviceOptions.TokenValidationParameters.IssuerSigningKeyResolver);
    }
}
