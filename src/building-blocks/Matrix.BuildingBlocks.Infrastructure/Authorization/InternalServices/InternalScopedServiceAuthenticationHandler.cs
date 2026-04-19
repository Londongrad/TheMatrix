using System.Net.Http.Headers;

namespace Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices
{
    public sealed class InternalScopedServiceAuthenticationHandler : DelegatingHandler
    {
        private readonly IInternalServiceJwtIssuer _jwtIssuer;
        private readonly IReadOnlyCollection<string> _permissions;
        private readonly string _serviceName;
        private readonly Guid _subjectId;

        public InternalScopedServiceAuthenticationHandler(
            IInternalServiceJwtIssuer jwtIssuer,
            Guid subjectId,
            string serviceName,
            IReadOnlyCollection<string> permissions)
        {
            _jwtIssuer = jwtIssuer;
            _subjectId = subjectId;
            _serviceName = serviceName;
            _permissions = permissions
               .Where(x => !string.IsNullOrWhiteSpace(x))
               .Distinct(StringComparer.Ordinal)
               .ToArray();
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: _jwtIssuer.Issue(
                    subjectId: _subjectId,
                    serviceName: _serviceName,
                    permissions: _permissions));

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
