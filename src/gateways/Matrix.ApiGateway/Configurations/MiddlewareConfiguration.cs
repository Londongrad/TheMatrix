using Matrix.BuildingBlocks.Api.Middleware;

namespace Matrix.ApiGateway.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.UseHttpsRedirection();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseCors("Frontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
        }
    }
}
