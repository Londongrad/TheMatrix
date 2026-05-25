using Matrix.ApiGateway.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.StartupTestSupport;

namespace Matrix.ApiGateway.Tests.Configurations
{
    public sealed class MiddlewareConfigurationTests
    {
        [Fact]
        public void ConfigureApplicationMiddleware_WhenApplied_MapsHealthAndControllerEndpoints()
        {
            WebApplicationBuilder builder = CreateBuilder(BuildValidApiConfiguration());
            builder.ConfigureApplicationServices();
            WebApplication app = builder.Build();

            app.ConfigureApplicationMiddleware();

            string[] routePatterns = ((IEndpointRouteBuilder)app).DataSources
               .SelectMany(static source => source.Endpoints)
               .OfType<RouteEndpoint>()
               .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
               .ToArray();

            Assert.Contains(
                expected: "/health/live",
                collection: routePatterns);
            Assert.Contains(
                expected: "/health/ready",
                collection: routePatterns);
        }
    }
}
