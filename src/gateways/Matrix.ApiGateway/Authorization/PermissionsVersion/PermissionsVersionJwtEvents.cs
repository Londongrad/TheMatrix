using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.Jwt;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public static class PermissionsVersionJwtEvents
    {
        public static async Task HandleTokenValidated(TokenValidatedContext context)
        {
            string? userIdValue = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                                  context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(
                    input: userIdValue,
                    result: out Guid userId))
            {
                context.Fail("invalid_token");
                return;
            }

            string? tokenPvValue = context.Principal?.FindFirstValue(JwtClaimNames.PermissionsVersion);

            if (!int.TryParse(
                    s: tokenPvValue,
                    result: out int tokenVersion))
            {
                context.Fail("invalid_token");
                return;
            }

            IPermissionsVersionStore store = context.HttpContext.RequestServices
               .GetRequiredService<IPermissionsVersionStore>();

            int currentVersion = await store.GetCurrentAsync(
                userId: userId,
                cancellationToken: context.HttpContext.RequestAborted);

            if (tokenVersion != currentVersion)
                MarkTokenStale(context);
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

        private static void MarkTokenStale(TokenValidatedContext context)
        {
            string? userId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                             context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            context.HttpContext.RequestServices
               .GetRequiredService<ILoggerFactory>()
               .CreateLogger("PV")
               .LogInformation(
                    message: "TOKEN STALE for userId={UserId}",
                    userId);

            context.HttpContext.Items[PermissionsVersionValidationDefaults.StaleTokenItemKey] = true;
            context.Fail("token_stale");
        }
    }
}
