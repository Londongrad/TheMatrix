using Matrix.BuildingBlocks.Api.Middleware;

namespace Matrix.Identity.Api.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.ConfigureControllers();
            app.UseSecurityPipeline();
            app.ConfigureMiddleware();
        }

        private static void UseSecurityPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
        }

        private static void ConfigureControllers(this WebApplication app) => app.MapControllers();

        private static void ConfigureMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseStaticFiles();
        }
    }
}
