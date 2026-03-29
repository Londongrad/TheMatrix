using System.Net.Http.Headers;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;

namespace Matrix.SimulationSystems.Infrastructure.Http
{
    internal sealed class InternalServiceAuthenticationHandler(IInternalServiceJwtIssuer jwtIssuer) : DelegatingHandler
    {
        private static readonly Guid SimulationSystemsServicePrincipalId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private readonly IInternalServiceJwtIssuer _jwtIssuer = jwtIssuer;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: _jwtIssuer.Issue(SimulationSystemsServicePrincipalId));

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
