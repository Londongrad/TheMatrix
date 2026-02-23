using Matrix.ApiGateway.Configurations.DependencyInjection;
using Matrix.BuildingBlocks.Api.Logging;

namespace Matrix.ApiGateway.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            builder.AddErrorFileLogging();

            builder.Services
               .AddGatewayCore()
               .AddGatewayAuth(builder.Configuration)
               .AddDownstreamServices(builder.Configuration)
               .AddBffFeatures(builder.Configuration);
        }
    }
}
