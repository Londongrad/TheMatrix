using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Economy.Api.Configurations;
using Matrix.Economy.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Economy.Api.Tests.TestSupport.EconomyApiTestSupport;

namespace Matrix.Economy.Api.Tests.Configurations
{
    public sealed class ServicesConfigurationTests
    {
        [Fact]
        public void
            ConfigureApplicationServices_WhenEconomyDbConnectionStringIsMissing_ThrowsInvalidOperationException()
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:EconomyDb"] = "",
                        ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                        ["InternalUserContextJwt:Audience"] = "economy-api",
                        ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                        ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                        ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                        ["InternalServiceJwt:Audience"] = "economy-api",
                        ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                        ["InternalServiceJwt:LifetimeSeconds"] = "300"
                    })
               .Build();
            WebApplicationBuilder builder = CreateBuilder(configuration);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                builder.ConfigureApplicationServices);

            Assert.Contains(
                expectedSubstring: "Connection string 'EconomyDb' is not configured",
                actualString: exception.Message);
        }

        [Fact]
        public void ConfigureApplicationServices_WhenConfigurationIsValid_RegistersApiAndInfrastructureServices()
        {
            WebApplicationBuilder builder = CreateBuilder(BuildValidApiConfiguration());

            builder.ConfigureApplicationServices();

            using ServiceProvider provider = builder.Services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            AuthenticationOptions authentication = provider.GetRequiredService<IOptions<AuthenticationOptions>>()
               .Value;

            Assert.Equal(
                expected: JwtAuthenticationExtensions.InternalCompositeJwtScheme,
                actual: authentication.DefaultAuthenticateScheme);
            Assert.Equal(
                expected: JwtAuthenticationExtensions.InternalCompositeJwtScheme,
                actual: authentication.DefaultChallengeScheme);
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUserContext>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityBudgetRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICityBusinessRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IEconomyUnitOfWork>());
            Assert.Same(
                expected: TimeProvider.System,
                actual: provider.GetRequiredService<TimeProvider>());
        }
    }
}
