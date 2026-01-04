using Matrix.BuildingBlocks.Api.Middleware;
using Matrix.Identity.Api.Authorization.Internal;

namespace Matrix.Identity.Api.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.UseCustomMiddleware();
            app.ConfigureMiddleware();
            app.UseSecurityPipeline();
            app.ConfigureControllers();
        }

        private static void UseCustomMiddleware(this WebApplication app)
        {
            app.UseMiddleware<InternalApiKeyMiddleware>();
        }

        private static void UseSecurityPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
        }

        private static void ConfigureControllers(this WebApplication app)
        {
            app.MapControllers();
        }

        private static void ConfigureMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseStaticFiles();
        }
    }
}
