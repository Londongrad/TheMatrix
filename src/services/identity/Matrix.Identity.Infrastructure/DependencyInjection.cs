using MassTransit;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Matrix.Identity.Infrastructure.Authorization;
using Matrix.Identity.Infrastructure.Integration.Email;
using Matrix.Identity.Infrastructure.Integration.GeoLocation;
using Matrix.Identity.Infrastructure.Integration.Links;
using Matrix.Identity.Infrastructure.Outbox.RabbitMq;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Persistence.Seed;
using Matrix.Identity.Infrastructure.Security.Audit;
using Matrix.Identity.Infrastructure.Security.Audit.Cleanup;
using Matrix.Identity.Infrastructure.Security.PasswordHashing;
using Matrix.Identity.Infrastructure.Security.Processor;
using Matrix.Identity.Infrastructure.Security.Tokens;
using Matrix.Identity.Infrastructure.Security.Tokens.Cleanup;
using Matrix.Identity.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            string? connectionString = configuration.GetConnectionString("IdentityDb");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'IdentityDb' is not configured.");

            services.AddPostgresResilienceOptions(configuration);

            services.AddDbContext<IdentityDbContext>((
                sp,
                options) =>
            {
                PostgresResilienceOptions resilience = sp.GetRequiredService<IOptions<PostgresResilienceOptions>>()
                   .Value;

                options.UseNpgsql(
                    connectionString: connectionString,
                    npgsqlOptionsAction: npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: resilience.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(resilience.MaxRetryDelaySeconds),
                        errorCodesToAdd: null));
            });

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
            services.AddScoped<ISecurityAuditBulkRepository, SecurityAuditBulkRepository>();
            services.AddScoped<ISecurityAuditReadRepository, SecurityAuditReadRepository>();
            services.AddScoped<IUserSessionRepository, UserSessionRepository>();
            services.AddScoped<IUserAdminReadRepository, UserAdminReadRepository>();
            services.AddScoped<IRoleMembersReadRepository, UserAdminReadRepository>();
            services.AddScoped<IDefaultUserAccessPolicyRepository, DefaultUserAccessPolicyRepository>();

            // Outbox pattern
            services.AddOutbox<IdentityDbContext>(configuration);
            // Validate options on start
            services.AddRabbitMqOptions(configuration);
            services.AddMassTransitEndpointHygieneOptions(configuration);
            services.AddScoped<IOutboxMessagePublisher, MassTransitOutboxMessagePublisher>();

            // Permission checker
            services.AddPermissionCheckingFromClaims();
            services.AddScoped<IPermissionChecker, DbFallbackPermissionChecker>();

            // Security services
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAccessTokenService, ExternalJwtAccessTokenService>();
            services.AddScoped<IRefreshTokenProvider, RefreshTokenProvider>();
            services.AddScoped<IOneTimeTokenService, OneTimeTokenService>();
            services.AddScoped<ISecurityAuditService, SecurityAuditService>();
            services.AddOptions<RefreshTokenCleanupOptions>()
               .Bind(configuration.GetSection(RefreshTokenCleanupOptions.SectionName))
               .Validate(
                    validation: o => o.PollIntervalSeconds > 0,
                    failureMessage:
                    $"{RefreshTokenCleanupOptions.SectionName}:PollIntervalSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.BatchSize > 0,
                    failureMessage:
                    $"{RefreshTokenCleanupOptions.SectionName}:BatchSize must be greater than 0.")
               .Validate(
                    validation: o => o.RevokedRetentionHours >= 0,
                    failureMessage:
                    $"{RefreshTokenCleanupOptions.SectionName}:RevokedRetentionHours must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.ExpiredRetentionHours >= 0,
                    failureMessage:
                    $"{RefreshTokenCleanupOptions.SectionName}:ExpiredRetentionHours must be greater than or equal to 0.")
               .ValidateOnStart();
            services.AddOptions<OneTimeTokenOptions>()
               .Bind(configuration.GetSection(OneTimeTokenOptions.SectionName))
               .Validate(
                    validation: o => o.EmailConfirmationLifetimeMinutes > 0,
                    failureMessage:
                    $"{OneTimeTokenOptions.SectionName}:EmailConfirmationLifetimeMinutes must be greater than 0.")
               .Validate(
                    validation: o => o.EmailConfirmationCooldownSeconds >= 0,
                    failureMessage:
                    $"{OneTimeTokenOptions.SectionName}:EmailConfirmationCooldownSeconds must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.EmailConfirmationMaxDeliveryAttemptsPerHour >= 0,
                    failureMessage:
                    $"{OneTimeTokenOptions.SectionName}:EmailConfirmationMaxDeliveryAttemptsPerHour must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.PasswordResetLifetimeMinutes > 0,
                    failureMessage:
                    $"{OneTimeTokenOptions.SectionName}:PasswordResetLifetimeMinutes must be greater than 0.")
               .Validate(
                    validation: o => o.PasswordResetCooldownSeconds >= 0,
                    failureMessage:
                    $"{OneTimeTokenOptions.SectionName}:PasswordResetCooldownSeconds must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.PasswordResetMaxDeliveryAttemptsPerHour >= 0,
                    failureMessage:
                    $"{OneTimeTokenOptions.SectionName}:PasswordResetMaxDeliveryAttemptsPerHour must be greater than or equal to 0.")
               .ValidateOnStart();
            services.AddOptions<SecurityAuditOptions>()
               .Bind(configuration.GetSection(SecurityAuditOptions.SectionName))
               .Validate(
                    validation: o => o.FailedLoginWindowMinutes > 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:FailedLoginWindowMinutes must be greater than 0.")
               .Validate(
                    validation: o => o.FailedLoginMaxAttemptsPerLogin >= 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:FailedLoginMaxAttemptsPerLogin must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.FailedLoginMaxAttemptsPerIp >= 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:FailedLoginMaxAttemptsPerIp must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.EmailConfirmationRequestWindowMinutes > 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:EmailConfirmationRequestWindowMinutes must be greater than 0.")
               .Validate(
                    validation: o => o.EmailConfirmationRequestMaxAttemptsPerEmail >= 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:EmailConfirmationRequestMaxAttemptsPerEmail must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.EmailConfirmationRequestMaxAttemptsPerIp >= 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:EmailConfirmationRequestMaxAttemptsPerIp must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.PasswordResetRequestWindowMinutes > 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:PasswordResetRequestWindowMinutes must be greater than 0.")
               .Validate(
                    validation: o => o.PasswordResetRequestMaxAttemptsPerEmail >= 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:PasswordResetRequestMaxAttemptsPerEmail must be greater than or equal to 0.")
               .Validate(
                    validation: o => o.PasswordResetRequestMaxAttemptsPerIp >= 0,
                    failureMessage:
                    $"{SecurityAuditOptions.SectionName}:PasswordResetRequestMaxAttemptsPerIp must be greater than or equal to 0.")
               .ValidateOnStart();
            services.AddOptions<SecurityAuditCleanupOptions>()
               .Bind(configuration.GetSection(SecurityAuditCleanupOptions.SectionName))
               .Validate(
                    validation: o => o.PollIntervalSeconds > 0,
                    failureMessage:
                    $"{SecurityAuditCleanupOptions.SectionName}:PollIntervalSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.BatchSize > 0,
                    failureMessage:
                    $"{SecurityAuditCleanupOptions.SectionName}:BatchSize must be greater than 0.")
               .Validate(
                    validation: o => o.RetentionDays >= 0,
                    failureMessage:
                    $"{SecurityAuditCleanupOptions.SectionName}:RetentionDays must be greater than or equal to 0.")
               .ValidateOnStart();
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<RefreshTokenCleaner>();
            services.AddScoped<SecurityAuditCleaner>();
            services.AddHostedService<RefreshTokenCleanupHostedService>();
            services.AddHostedService<SecurityAuditCleanupHostedService>();

            // Security state change processing
            services.AddScoped<ISecurityStateChangeProcessor, SecurityStateChangeProcessor>();

            services.AddOptions<EmailOptions>()
               .Bind(configuration.GetSection(EmailOptions.SectionName))
               .Validate(
                    validation: o => Enum.IsDefined(o.DeliveryMode),
                    failureMessage: $"{EmailOptions.SectionName}:DeliveryMode is invalid.")
               .Validate(
                    validation: o
                        => o.DeliveryMode != EmailDeliveryMode.Smtp || !string.IsNullOrWhiteSpace(o.FromEmail),
                    failureMessage: $"{EmailOptions.SectionName}:FromEmail is required when SMTP delivery is enabled.")
               .Validate(
                    validation: o => o.DeliveryMode != EmailDeliveryMode.Smtp || !string.IsNullOrWhiteSpace(o.SmtpHost),
                    failureMessage: $"{EmailOptions.SectionName}:SmtpHost is required when SMTP delivery is enabled.")
               .Validate(
                    validation: o => o.DeliveryMode != EmailDeliveryMode.Smtp || o.SmtpPort > 0,
                    failureMessage:
                    $"{EmailOptions.SectionName}:SmtpPort must be greater than 0 when SMTP delivery is enabled.")
               .ValidateOnStart();
            services.AddOptions<FrontendLinksOptions>()
               .Bind(configuration.GetSection(FrontendLinksOptions.SectionName))
               .Validate(
                    validation: o => Uri.TryCreate(
                        uriString: o.BaseUrl,
                        uriKind: UriKind.Absolute,
                        result: out _),
                    failureMessage: $"{FrontendLinksOptions.SectionName}:BaseUrl must be an absolute URI.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.ConfirmEmailPath),
                    failureMessage: $"{FrontendLinksOptions.SectionName}:ConfirmEmailPath is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.ResetPasswordPath),
                    failureMessage: $"{FrontendLinksOptions.SectionName}:ResetPasswordPath is required.")
               .ValidateOnStart();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IFrontendLinkBuilder, FrontendLinkBuilder>();

            services.AddScoped<IAvatarStorage, FileSystemAvatarStorage>();

            // Authorization
            services.AddScoped<IEffectivePermissionsService, EffectivePermissionsService>();

            // Seeding
            services.AddScoped<PermissionsSeeder>();
            services.AddScoped<RolesSeeder>();
            services.AddScoped<DefaultUserAccessPolicySeeder>();
            services.AddScoped<BootstrapSuperAdminSeeder>();

            // External services
            services.AddGeoLocation(configuration);

            // MassTransit (RabbitMQ) integration
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddRabbitMqEndpointHygiene();

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
