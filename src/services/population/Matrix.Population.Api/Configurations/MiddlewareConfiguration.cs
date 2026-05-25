using Matrix.BuildingBlocks.Api.Defaults;

namespace Matrix.Population.Api.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.UseMatrixApi();
        }
    }
}
