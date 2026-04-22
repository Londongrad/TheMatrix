using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices
{
    public static class InternalServiceHttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddInternalServiceAuthentication(
            this IHttpClientBuilder builder,
            InternalServiceIdentity identity,
            params string[] permissions)
        {
            return builder.AddHttpMessageHandler(sp => new InternalScopedServiceAuthenticationHandler(
                jwtIssuer: sp.GetRequiredService<IInternalServiceJwtIssuer>(),
                subjectId: identity.SubjectId,
                serviceName: identity.ServiceName,
                permissions: permissions));
        }
    }
}
