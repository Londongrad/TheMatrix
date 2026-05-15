using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Api.Configurations;
using Matrix.Resources.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Resources.Api.Tests.TestSupport.ResourcesApiTestSupport;

namespace Matrix.Resources.Api.Tests.Configurations;

public sealed class ServicesConfigurationTests
{
    [Fact]
    public void ConfigureApplicationServices_WhenResourcesDbConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ResourcesDb"] = "",
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "resources-api",
                ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "resources-api",
                ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                ["InternalServiceJwt:LifetimeSeconds"] = "300"
            })
            .Build();
        WebApplicationBuilder builder = CreateBuilder(configuration);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(builder.ConfigureApplicationServices);

        Assert.Contains("Connection string 'ResourcesDb' is not configured", exception.Message);
    }

    [Fact]
    public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersApiInfrastructureAndTypedClients()
    {
        WebApplicationBuilder builder = CreateBuilder(BuildValidApiConfiguration());

        builder.ConfigureApplicationServices();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        Assert.Equal(JwtAuthenticationExtensions.InternalCompositeJwtScheme, authentication.DefaultAuthenticateScheme);
        Assert.Equal(JwtAuthenticationExtensions.InternalCompositeJwtScheme, authentication.DefaultChallengeScheme);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUserContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityStockpileRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityBudgetAuthorizationClient>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityResupplyTripDispatcher>());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }
}
