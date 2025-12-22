using System.Text.Json;
using Matrix.ApiGateway.DownstreamClients.Common.Errors;

namespace Matrix.ApiGateway.DownstreamClients.Common.Extensions
{
    public static class HttpResponseMessageJsonExtensions
    {
        public static async Task<T> ReadJsonOrThrowDownstreamAsync<T>(
            this HttpResponseMessage response,
            string serviceName,
            CancellationToken ct,
            string? requestUrl = null)
            where T : class
        {
            await response.EnsureSuccessOrThrowDownstreamAsync(serviceName, ct);

            try
            {
                T? dto = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);

                if (dto is null)
                    throw DownstreamClientErrorFactory.InvalidResponseBody(
                        serviceName: serviceName,
                        requestUrl: requestUrl ?? response.RequestMessage?.RequestUri?.ToString(),
                        expected: typeof(T).Name);

                return dto;
            }
            catch (JsonException ex)
            {
                throw DownstreamClientErrorFactory.InvalidJson(
                    serviceName: serviceName,
                    requestUrl: requestUrl ?? response.RequestMessage?.RequestUri?.ToString(),
                    expected: typeof(T).Name,
                    inner: ex);
            }
            catch (NotSupportedException ex)
            {
                // например, content-type вообще не JSON
                throw DownstreamClientErrorFactory.InvalidJson(
                    serviceName: serviceName,
                    requestUrl: requestUrl ?? response.RequestMessage?.RequestUri?.ToString(),
                    expected: typeof(T).Name,
                    inner: ex);
            }
        }
    }
}
