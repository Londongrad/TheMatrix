using System.Net.Http.Headers;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;

namespace Matrix.Population.Infrastructure.Http
{
    internal sealed class InternalServiceAuthenticationHandler(IInternalServiceJwtIssuer jwtIssuer) : DelegatingHandler
    {
        private static readonly Guid PopulationServicePrincipalId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private readonly IInternalServiceJwtIssuer _jwtIssuer = jwtIssuer;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: _jwtIssuer.Issue(PopulationServicePrincipalId));

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
