using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Middleware;

namespace Matrix.SimulationSystems.Api.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseSecurityPipeline();
            app.ConfigureControllers();
        }

        private static void UseSecurityPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
        }

        private static void ConfigureControllers(this WebApplication app)
        {
            app.MapOperationalHealthChecks();
            app.MapControllers();
        }
    }
}
