using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Api.Configurations;
using Matrix.Population.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Population.Api.Tests.TestSupport.PopulationApiTestSupport;

namespace Matrix.Population.Api.Tests.Configurations;

public sealed class ServicesConfigurationTests
{
    [Fact]
    public void ConfigureApplicationServices_WhenPopulationDbConnectionStringIsMissing_ThrowsInvalidOperationException()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PopulationDb"] = "",
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "population-api",
                ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "population-api",
                ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                ["InternalServiceJwt:LifetimeSeconds"] = "300"
            })
            .Build();
        WebApplicationBuilder builder = CreateBuilder(configuration);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            builder.ConfigureApplicationServices);

        Assert.Contains("Connection string 'PopulationDb' is not configured", exception.Message);
    }

    [Fact]
    public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersApiAndInfrastructureServices()
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
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPersonReadRepository>());
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }
}
