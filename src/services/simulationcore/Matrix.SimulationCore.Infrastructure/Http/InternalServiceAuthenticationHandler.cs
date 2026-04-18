using System.Net.Http.Headers;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;

namespace Matrix.SimulationCore.Infrastructure.Http
{
    internal sealed class InternalServiceAuthenticationHandler(IInternalServiceJwtIssuer jwtIssuer) : DelegatingHandler
    {
        private static readonly Guid SimulationCoreServicePrincipalId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private readonly IInternalServiceJwtIssuer _jwtIssuer = jwtIssuer;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: _jwtIssuer.Issue(SimulationCoreServicePrincipalId));

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
