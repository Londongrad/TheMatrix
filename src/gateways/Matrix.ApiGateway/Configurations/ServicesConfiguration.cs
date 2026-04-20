using Matrix.ApiGateway.Configurations.DependencyInjection;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Logging;
using Matrix.BuildingBlocks.Api.Forwarding;

namespace Matrix.ApiGateway.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            builder.AddSerilogLogging();

            builder.Services
               .AddOperationalHealthChecks(builder.Configuration)
               .AddTrustedForwardedHeaders(builder.Configuration)
               .AddGatewayCore(builder.Configuration)
               .AddGatewayAuth(builder.Configuration)
               .AddDownstreamServices(builder.Configuration)
               .AddBffFeatures(builder.Configuration);
        }
    }
}
