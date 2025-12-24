using System.Net.Http.Headers;

namespace Matrix.ApiGateway.DownstreamClients.Common.Extensions
{
    public static class HttpClientMultipartExtensions
    {
        public static async Task<HttpResponseMessage> PutMultipartFileAsync(
            this HttpClient client,
            string requestUri,
            string formFieldName,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            return await client.SendMultipartFileAsync(
                method: HttpMethod.Put,
                requestUri: requestUri,
                formFieldName: formFieldName,
                file: file,
                cancellationToken: cancellationToken);
        }

        public static async Task<HttpResponseMessage> PostMultipartFileAsync(
            this HttpClient client,
            string requestUri,
            string formFieldName,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            return await client.SendMultipartFileAsync(
                method: HttpMethod.Post,
                requestUri: requestUri,
                formFieldName: formFieldName,
                file: file,
                cancellationToken: cancellationToken);
        }

        public static async Task<HttpResponseMessage> SendMultipartFileAsync(
            this HttpClient client,
            HttpMethod method,
            string requestUri,
            string formFieldName,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(client);

            // Важно: контент и stream должны жить до завершения SendAsync.
            using var content = new MultipartFormDataContent();

            await using Stream stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

            content.Add(
                content: fileContent,
                name: formFieldName,
                fileName: file.FileName);

            using var request = new HttpRequestMessage(
                method: method,
                requestUri: requestUri)
            {
                Content = content
            };

            // Возвращаем response наружу — вызывающий сам его Dispose'ит через using.
            return await client.SendAsync(
                request: request,
                completionOption: HttpCompletionOption.ResponseHeadersRead,
                cancellationToken: cancellationToken);
        }
    }
}
