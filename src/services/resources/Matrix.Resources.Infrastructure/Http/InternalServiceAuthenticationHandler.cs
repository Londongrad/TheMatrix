using System.Net.Http.Headers;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;

namespace Matrix.Resources.Infrastructure.Http
{
    internal sealed class InternalServiceAuthenticationHandler(IInternalServiceJwtIssuer jwtIssuer) : DelegatingHandler
    {
        private static readonly Guid ResourcesServicePrincipalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private readonly IInternalServiceJwtIssuer _jwtIssuer = jwtIssuer;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: _jwtIssuer.Issue(ResourcesServicePrincipalId));

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
