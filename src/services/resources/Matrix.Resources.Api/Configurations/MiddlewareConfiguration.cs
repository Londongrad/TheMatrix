using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Middleware;
using Matrix.BuildingBlocks.Api.Defaults;

namespace Matrix.Resources.Api.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.UseMatrixApi();
        }
    }
}
