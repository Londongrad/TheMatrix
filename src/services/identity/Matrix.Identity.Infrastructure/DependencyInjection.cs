using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Infrastructure.Authentication.Jwt;
using Matrix.Identity.Infrastructure.Integration.GeoLocation;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Security;
using Matrix.Identity.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Identity.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // DbContext
            string connectionString = configuration.GetConnectionString("IdentityDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'IdentityDb' is not configured.");

            services.AddDbContext<IdentityDbContext>(options => { options.UseNpgsql(connectionString); });

            // Jwt options
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();

            // Security services
            services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
            services.AddScoped<IAccessTokenService, JwtAccessTokenService>();
            services.AddScoped<IRefreshTokenProvider, RefreshTokenProvider>();

            services.AddScoped<IAvatarStorage, FileSystemAvatarStorage>();

            // External services
            services.AddGeoLocation(configuration);

            return services;
        }

        private static IServiceCollection AddGeoLocation(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<GeoLocationOptions>(configuration.GetSection(GeoLocationOptions.SectionName));

            // HttpClient factory + IGeoLocationService
            services.AddHttpClient<IGeoLocationService, GeoLocationService>();

            return services;
        }
    }
}
