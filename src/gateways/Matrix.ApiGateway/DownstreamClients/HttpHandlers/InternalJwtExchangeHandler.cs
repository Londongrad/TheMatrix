using System.Net.Http.Headers;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.Identity.Contracts.Internal.Responses;

namespace Matrix.ApiGateway.DownstreamClients.HttpHandlers
{
    public sealed class InternalJwtExchangeHandler(
        IHttpContextAccessor http,
        IPermissionsVersionStore pvStore,
        IAuthContextStore authContextStore,
        IInternalJwtIssuer internalJwtIssuer)
        : DelegatingHandler
    {
        private readonly IAuthContextStore _authContextStore = authContextStore;
        private readonly IHttpContextAccessor _http = http;
        private readonly IInternalJwtIssuer _internalJwtIssuer = internalJwtIssuer;
        private readonly IPermissionsVersionStore _pvStore = pvStore;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ClaimsPrincipal? user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return await base.SendAsync(
                    request: request,
                    cancellationToken: cancellationToken);

            // 1) userId из внешнего access JWT
            string? sub = user.FindFirst("sub")
              ?.Value;

            if (!Guid.TryParse(
                    input: sub,
                    result: out Guid userId))
                return await base.SendAsync(
                    request: request,
                    cancellationToken: cancellationToken);

            // 2) получаем актуальный permissionsVersion (у тебя уже есть Redis+fallback Identity)
            int currentPv = await _pvStore.GetCurrentAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            // 3) получаем effective permissions (кэшируется в Redis внутри CachedAuthContextStore)
            UserAuthContextResponse ctx = await _authContextStore.GetAsync(
                userId: userId,
                permissionsVersion: currentPv,
                ct: cancellationToken);

            // 4) достаем jti
            string? jti = _http.HttpContext?.User.FindFirst("jti")
              ?.Value;

            // 5) выпускаем internal JWT (короткий TTL) с perm-claims
            string internalJwt = _internalJwtIssuer.Issue(
                userId: userId,
                jti: jti,
                permissionsVersion: ctx.PermissionsVersion, // обычно совпадает с currentPv
                permissions: ctx.EffectivePermissions);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    scheme: "Bearer",
                    parameter: internalJwt);

            return await base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
