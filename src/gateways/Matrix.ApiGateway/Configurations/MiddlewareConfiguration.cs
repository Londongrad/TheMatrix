namespace Matrix.ApiGateway.Configurations
{
    public static class MiddlewareConfiguration
    {
        public static void ConfigureApplicationMiddleware(this WebApplication app)
        {
            app.UseSecurityPipeline();
            app.UseCors("Frontend");
        }

        private static void UseSecurityPipeline(this WebApplication app)
        {
            app.UseHttpsRedirection();
            //app.UseAuthentication();
            //app.UseAuthorization();
        }
    }
}
