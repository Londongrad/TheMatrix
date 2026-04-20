using Matrix.ApiGateway.Configurations.DependencyInjection;
using Matrix.ApiGateway.Configurations.Security;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Api.Middleware;

namespace Matrix.ApiGateway.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.UseTrustedForwardedHeaders();
            app.UseHttpsRedirection();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseCors(GatewayCorsDefaults.PolicyName);
            app.UseMiddleware<BrowserCookieRequestProtectionMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapOperationalHealthChecks();
            app.MapControllers();
        }
    }
}
