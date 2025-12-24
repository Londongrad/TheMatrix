using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;

namespace Matrix.ApiGateway.DownstreamClients.Common.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task EnsureSuccessOrThrowDownstreamAsync(
            this HttpResponseMessage response,
            string serviceName,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            string? contentType = response.Content.Headers.ContentType?.ToString();

            string? url = response.RequestMessage?.RequestUri?.ToString();

            throw new DownstreamServiceException(
                serviceName: serviceName,
                statusCode: response.StatusCode,
                body: body,
                contentType: contentType,
                requestUrl: url);
        }
    }
}
