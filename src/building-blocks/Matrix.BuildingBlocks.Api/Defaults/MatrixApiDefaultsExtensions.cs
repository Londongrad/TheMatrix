using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Logging;
using Matrix.BuildingBlocks.Api.Middleware;
using Matrix.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Api.Defaults
{
    public static class MatrixApiDefaultsExtensions
    {
        public static WebApplicationBuilder AddMatrixServiceDefaults(this WebApplicationBuilder builder)
        {
            builder.AddSerilogLogging();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddOperationalHealthChecks(builder.Configuration);

            return builder;
        }

        public static IServiceCollection AddMatrixInternalApi(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddInternalJwtAuthentication(configuration);
            services.AddAuthorization();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            return services;
        }

        public static WebApplication UseMatrixApi(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapOperationalHealthChecks();
            app.MapControllers();

            return app;
        }
    }
}
