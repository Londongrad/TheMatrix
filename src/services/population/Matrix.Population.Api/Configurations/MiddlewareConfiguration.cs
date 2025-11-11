namespace Matrix.Population.Api.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.ConfigureControllers();
            app.UseSecurityPipeline();
        }

        private static void UseSecurityPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();
            //app.UseAuthentication();
            //app.UseAuthorization();
        }

        private static void ConfigureControllers(this WebApplication app)
        {
            app.UseCors("Frontend");
            app.MapControllers();
        }
    }
}
