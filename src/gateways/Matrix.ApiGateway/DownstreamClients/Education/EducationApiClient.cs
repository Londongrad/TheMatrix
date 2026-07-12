using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Education.Contracts;
using Matrix.Education.Contracts.Enrollments;
using Matrix.Education.Contracts.Institutions;

namespace Matrix.ApiGateway.DownstreamClients.Education
{
    internal sealed class EducationApiClient(HttpClient client) : IEducationApiClient
    {
        private const string ServiceName = DownstreamServiceNames.Education;
        private const string SimulationHostRouteParameter = "{simulationHostId:guid}";
        private readonly HttpClient _client = client;

        public async Task<EducationInstitutionCatalogResponse> ListInstitutionsAsync(
            Guid simulationHostId,
            CancellationToken cancellationToken = default)
        {
            string url = ResolveRoute(
                routeTemplate: EducationApiRoutes.Institutions,
                simulationHostId: simulationHostId);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<EducationInstitutionCatalogResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<SynchronizeEducationInstitutionsResponse> SynchronizeInstitutionsAsync(
            Guid simulationHostId,
            SynchronizeEducationInstitutionsRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = ResolveRoute(
                routeTemplate: EducationApiRoutes.Institutions,
                simulationHostId: simulationHostId);

            using HttpResponseMessage response = await _client.PutAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<SynchronizeEducationInstitutionsResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<EducationEnrollmentOperationResponse> EnrollStudentAsync(
            Guid simulationHostId,
            EnrollStudentRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = ResolveRoute(
                routeTemplate: EducationApiRoutes.Enrollments,
                simulationHostId: simulationHostId);

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await ReadEnrollmentResponseAsync(
                response: response,
                url: url,
                cancellationToken: cancellationToken);
        }

        public async Task<EducationEnrollmentOperationResponse> CompleteStudentStageAsync(
            Guid simulationHostId,
            CompleteStudentStageRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{ResolveRoute(EducationApiRoutes.Enrollments, simulationHostId)}/complete";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await ReadEnrollmentResponseAsync(
                response: response,
                url: url,
                cancellationToken: cancellationToken);
        }

        public async Task<EducationEnrollmentOperationResponse> WithdrawStudentAsync(
            Guid simulationHostId,
            WithdrawStudentRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"{ResolveRoute(EducationApiRoutes.Enrollments, simulationHostId)}/withdraw";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await ReadEnrollmentResponseAsync(
                response: response,
                url: url,
                cancellationToken: cancellationToken);
        }

        private static string ResolveRoute(
            string routeTemplate,
            Guid simulationHostId)
        {
            return routeTemplate.Replace(
                oldValue: SimulationHostRouteParameter,
                newValue: simulationHostId.ToString("D"),
                comparisonType: StringComparison.Ordinal);
        }

        private static Task<EducationEnrollmentOperationResponse> ReadEnrollmentResponseAsync(
            HttpResponseMessage response,
            string url,
            CancellationToken cancellationToken)
        {
            return response.ReadJsonOrThrowDownstreamAsync<EducationEnrollmentOperationResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }
    }
}
