using Matrix.Population.Application;
using Matrix.Population.Infrastructure;

namespace Matrix.Population.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            builder.Services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
