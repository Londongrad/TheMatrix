using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.Identity.Contracts.Internal.Responses;

namespace Matrix.ApiGateway.DownstreamClients.HttpHandlers
{
    public sealed class InternalJwtExchangeHandler(
        IHttpContextAccessor http,
        IPermissionsVersionStore pvStore,
        IAuthContextStore authContextStore,
        IInternalJwtIssuer internalJwtIssuer,
        IInternalJwtRequestContextAccessor requestContextAccessor)
        : DelegatingHandler
    {
        private readonly IAuthContextStore _authContextStore = authContextStore;
        private readonly IHttpContextAccessor _http = http;
        private readonly IInternalJwtIssuer _internalJwtIssuer = internalJwtIssuer;
        private readonly IPermissionsVersionStore _pvStore = pvStore;
        private readonly IInternalJwtRequestContextAccessor _requestContextAccessor = requestContextAccessor;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ClaimsPrincipal? user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                string? sub =
                    user.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                    user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (Guid.TryParse(
                        input: sub,
                        result: out Guid userId))
                {
                    int currentPv = await _pvStore.GetCurrentAsync(
                        userId: userId,
                        cancellationToken: cancellationToken);

                    UserAuthContextResponse ctx = await _authContextStore.GetAsync(
                        userId: userId,
                        permissionsVersion: currentPv,
                        ct: cancellationToken);

                    string? jti = _http.HttpContext?.User.FindFirst("jti")
                      ?.Value;

                    string internalJwt = _internalJwtIssuer.Issue(
                        userId: userId,
                        jti: jti,
                        permissionsVersion: ctx.PermissionsVersion,
                        permissions: ctx.EffectivePermissions);

                    request.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            scheme: "Bearer",
                            parameter: internalJwt);
                }
            }
            else
                if (_requestContextAccessor.Current is
                    { } requestContext)
            {
                string internalJwt = _internalJwtIssuer.Issue(
                    userId: requestContext.UserId,
                    jti: requestContext.Jti,
                    permissionsVersion: requestContext.PermissionsVersion,
                    permissions: requestContext.EffectivePermissions);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        scheme: "Bearer",
                        parameter: internalJwt);
            }

            return await base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
