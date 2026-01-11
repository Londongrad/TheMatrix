using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Infrastructure.Logging;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public static class PermissionsVersionJwtEvents
    {
        private const string LoggerCategory = "Matrix.ApiGateway.Authorization.PermissionsVersion.JwtEvents";

        public static async Task HandleTokenValidated(TokenValidatedContext context)
        {
            ILogger logger = GetLogger(context.HttpContext);

            string? userIdValue =
                context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(
                    input: userIdValue,
                    result: out Guid userId))
            {
                if (LogRateLimiter.ShouldLog(
                        key: LogKeys.InvalidUserId,
                        period: RateLimit.Period))
                    logger.LogWarning(
                        message: "Invalid token: user id claim is missing or not a GUID. TraceId={TraceId} Path={Path}",
                        context.HttpContext.TraceIdentifier,
                        context.HttpContext.Request.Path.Value);

                context.Fail(FailReasons.InvalidToken);
                return;
            }

            string? tokenPvValue = context.Principal?.FindFirstValue(JwtClaimNames.PermissionsVersion);

            if (!int.TryParse(
                    s: tokenPvValue,
                    result: out int tokenVersion))
            {
                if (LogRateLimiter.ShouldLog(
                        key: LogKeys.InvalidPv,
                        period: RateLimit.Period))
                    logger.LogWarning(
                        message:
                        "Invalid token: permissions version claim is missing or not an integer. UserId={UserId} TraceId={TraceId} Path={Path}",
                        userId,
                        context.HttpContext.TraceIdentifier,
                        context.HttpContext.Request.Path.Value);

                context.Fail(FailReasons.InvalidToken);
                return;
            }

            IPermissionsVersionStore store = context.HttpContext.RequestServices
               .GetRequiredService<IPermissionsVersionStore>();

            int currentVersion = await store.GetCurrentAsync(
                userId: userId,
                cancellationToken: context.HttpContext.RequestAborted);

            if (tokenVersion != currentVersion)
                MarkTokenStale(
                    context: context,
                    logger: logger,
                    userId: userId,
                    tokenVersion: tokenVersion,
                    currentVersion: currentVersion);
        }

        public static async Task HandleChallenge(JwtBearerChallengeContext context)
        {
            if (!context.HttpContext.Items.TryGetValue(
                    key: PermissionsVersionValidationDefaults.StaleTokenItemKey,
                    value: out object? flag) ||
                flag is not true)
                return;

            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                code = PermissionsVersionValidationDefaults.TokenStaleErrorCode,
                message = PermissionsVersionValidationDefaults.TokenStaleMessage
            };

            await context.Response.WriteAsJsonAsync(
                value: payload,
                cancellationToken: context.HttpContext.RequestAborted);
        }

        private static ILogger GetLogger(HttpContext httpContext)
        {
            ILoggerFactory factory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            return factory.CreateLogger(LoggerCategory);
        }

        private static void MarkTokenStale(
            TokenValidatedContext context,
            ILogger logger,
            Guid userId,
            int tokenVersion,
            int currentVersion)
        {
            if (LogRateLimiter.ShouldLog(
                    key: LogKeys.StaleToken,
                    period: RateLimit.Period))
                logger.LogInformation(
                    message:
                    "Stale token detected. UserId={UserId} TokenVersion={TokenVersion} CurrentVersion={CurrentVersion} TraceId={TraceId} Path={Path}",
                    userId,
                    tokenVersion,
                    currentVersion,
                    context.HttpContext.TraceIdentifier,
                    context.HttpContext.Request.Path.Value);

            context.HttpContext.Items[PermissionsVersionValidationDefaults.StaleTokenItemKey] = true;
            context.Fail(FailReasons.TokenStale);
        }

        private static class RateLimit
        {
            internal static readonly TimeSpan Period = TimeSpan.FromSeconds(15);
        }

        private static class LogKeys
        {
            internal const string InvalidUserId = "pv.jwt.invalid.userId";
            internal const string InvalidPv = "pv.jwt.invalid.pv";
            internal const string StaleToken = "pv.jwt.stale";
        }

        private static class FailReasons
        {
            internal const string InvalidToken = "invalid_token";
            internal const string TokenStale = "token_stale";
        }
    }
}
