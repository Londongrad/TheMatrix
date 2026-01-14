using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Matrix.Identity.Infrastructure.Authorization;
using Matrix.Identity.Infrastructure.Integration.Email;
using Matrix.Identity.Infrastructure.Integration.GeoLocation;
using Matrix.Identity.Infrastructure.Integration.Links;
using Matrix.Identity.Infrastructure.Outbox;
using Matrix.Identity.Infrastructure.Outbox.Abstractions;
using Matrix.Identity.Infrastructure.Outbox.Postgres;
using Matrix.Identity.Infrastructure.Outbox.RabbitMq;
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
using Microsoft.Extensions.Options;

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
            services.AddOptions<ExternalJwtOptions>()
               .Bind(configuration.GetSection(ExternalJwtOptions.SectionName))
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Issuer),
                    failureMessage: $"{ExternalJwtOptions.SectionName}:Issuer is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Audience),
                    failureMessage: $"{ExternalJwtOptions.SectionName}:Audience is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.SigningKey),
                    failureMessage: $"{ExternalJwtOptions.SectionName}:SigningKey is required.")
               .Validate(
                    validation: o => o.AccessTokenLifetimeMinutes > 0,
                    failureMessage:
                    $"{ExternalJwtOptions.SectionName}:AccessTokenLifetimeMinutes must be greater than 0.")
               .Validate(
                    validation: o => o.RefreshTokenLifetimeDays > 0,
                    failureMessage:
                    $"{ExternalJwtOptions.SectionName}:RefreshTokenLifetimeDays must be greater than 0.")
               .Validate(
                    validation: o => o.ShortRefreshTokenLifetimeHours > 0,
                    failureMessage:
                    $"{ExternalJwtOptions.SectionName}:ShortRefreshTokenLifetimeHours must be greater than 0.")
               .ValidateOnStart();

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

            // Outbox pattern
            services.AddHostedService<OutboxPublisherBackgroundService>();
            services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));
            // Validate options on start
            services.AddOptions<RabbitMqOptions>()
               .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Host),
                    failureMessage: "RabbitMq:Host is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Username),
                    failureMessage: "RabbitMq:Username is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Password),
                    failureMessage: "RabbitMq:Password is required.")
               .ValidateOnStart();
            services.AddScoped<IOutboxRepository, PostgresOutboxRepository>();
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();

            // Permission checker
            services.AddPermissionCheckingFromClaims(); // Включаем общий claims-checker
            services.AddScoped<IPermissionChecker, DbFallbackPermissionChecker>(); // Identity специфичный чекер

            // Security services
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAccessTokenService, ExternalJwtAccessTokenService>();
            services.AddScoped<IRefreshTokenProvider, RefreshTokenProvider>();
            services.AddScoped<IOneTimeTokenService, OneTimeTokenService>();

            // Security state change processing
            services.AddScoped<ISecurityStateChangeProcessor, SecurityStateChangeProcessor>();

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
                    RabbitMqOptions rmq = context.GetRequiredService<IOptions<RabbitMqOptions>>()
                       .Value;

                    cfg.Host(
                        host: rmq.Host,
                        port: rmq.Port,
                        virtualHost: rmq.VirtualHost,
                        configure: h =>
                        {
                            h.Username(rmq.Username);
                            h.Password(rmq.Password);
                        });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }

        private static IServiceCollection AddGeoLocation(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<GeoLocationOptions>()
               .Bind(configuration.GetSection(GeoLocationOptions.SectionName))
               .Validate(
                    validation: o => o.TimeoutSeconds > 0,
                    failureMessage: "GeoLocation:TimeoutSeconds must be greater than 0.")
               .Validate(
                    validation: o => !o.Enabled || !string.IsNullOrWhiteSpace(o.EndpointTemplate),
                    failureMessage: "GeoLocation:EndpointTemplate is required when GeoLocation is enabled.")
               .Validate(
                    validation: o => !o.Enabled ||
                                     o.EndpointTemplate.Contains(
                                         value: "{ip}",
                                         comparisonType: StringComparison.Ordinal),
                    failureMessage:
                    "GeoLocation:EndpointTemplate must contain '{ip}' placeholder when GeoLocation is enabled.")
               .ValidateOnStart();

            services.AddHttpClient<IGeoLocationService, GeoLocationService>((
                sp,
                client) =>
            {
                GeoLocationOptions options = sp.GetRequiredService<IOptions<GeoLocationOptions>>()
                   .Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

            return services;
        }
    }
}
