using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Infrastructure.Authentication.Jwt;
using Matrix.Identity.Infrastructure.Authorization;
using Matrix.Identity.Infrastructure.Integration.Email;
using Matrix.Identity.Infrastructure.Integration.GeoLocation;
using Matrix.Identity.Infrastructure.Integration.Links;
using Matrix.Identity.Infrastructure.Outbox;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Persistence.Seed;
using Matrix.Identity.Infrastructure.Security.PasswordHashing;
using Matrix.Identity.Infrastructure.Security.Processor;
using Matrix.Identity.Infrastructure.Security.Tokens;
using Matrix.Identity.Infrastructure.Storage;
using Matrix.Identity.Infrastructure.Time;
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
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOneTimeTokenRepository, OneTimeTokenRepository>();
            services.AddScoped<IRoleReadRepository, RoleReadRepository>();
            services.AddScoped<IRoleWriteRepository, RoleWriteRepository>();
            services.AddScoped<IPermissionReadRepository, PermissionReadRepository>();
            services.AddScoped<IUserRolesRepository, UserRolesRepository>();
            services.AddScoped<IUserPermissionsRepository, UserPermissionsRepository>();
            services.AddScoped<IRolePermissionsRepository, RolePermissionsRepository>();
            services.AddScoped<IRefreshTokenBulkRepository, RefreshTokenBulkRepository>();
            services.AddScoped<IUserAdminReadRepository, UserAdminReadRepository>();
            services.AddScoped<IRoleMembersReadRepository, UserAdminReadRepository>();

            // Security services
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAccessTokenService, JwtAccessTokenService>();
            services.AddScoped<IRefreshTokenProvider, RefreshTokenProvider>();
            services.AddScoped<IOneTimeTokenService, OneTimeTokenService>();

            // Security state change processing + outbox publisher
            services.AddScoped<ISecurityStateChangeProcessor, SecurityStateChangeProcessor>();
            services.AddHostedService<OutboxPublisherBackgroundService>();

            services.AddScoped<IClock, SystemClock>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IFrontendLinkBuilder, FrontendLinkBuilder>();

            services.AddScoped<IAvatarStorage, FileSystemAvatarStorage>();

            // Authorization
            services.AddScoped<IEffectivePermissionsService, EffectivePermissionsService>();

            // Seeding
            services.AddScoped<PermissionsSeeder>();
            services.AddScoped<RolesSeeder>();
            services.AddScoped<BootstrapAdminSeeder>();

            // External services
            services.AddGeoLocation(configuration);

            // MassTransit (RabbitMQ) integration
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.UsingRabbitMq((
                    context,
                    cfg) =>
                {
                    cfg.Host(
                        host: "localhost",
                        virtualHost: "/",
                        configure: h =>
                        {
                            h.Username("admin");
                            h.Password("admin");
                        });
                });
            });

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
