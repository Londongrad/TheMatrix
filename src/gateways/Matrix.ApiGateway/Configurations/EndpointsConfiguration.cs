namespace Matrix.ApiGateway.Configurations
{
    public static class EndpointsConfiguration
    {
        public static void ConfigureApplicationEndpoints(this WebApplication app)
        {
            // Controllers endpoints
            app.MapControllers();
        }
    }
}