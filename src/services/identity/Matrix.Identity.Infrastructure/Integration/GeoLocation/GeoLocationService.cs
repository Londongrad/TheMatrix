using Matrix.Identity.Application.Abstractions;
using DomainGeoLocation = Matrix.Identity.Domain.ValueObjects.GeoLocation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;

namespace Matrix.Identity.Infrastructure.Integration.GeoLocation
{
    /// <summary>
    /// HTTP-based implementation of IGeoLocationService using external GeoIP provider.
    /// </summary>
    public sealed class GeoLocationService : IGeoLocationService
    {
        private readonly HttpClient _httpClient;
        private readonly GeoLocationOptions _options;
        private readonly ILogger<GeoLocationService> _logger;

        public GeoLocationService(
            HttpClient httpClient,
            IOptions<GeoLocationOptions> options,
            ILogger<GeoLocationService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        }

        public async Task<DomainGeoLocation?> ResolveAsync(
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            // Feature toggle
            if (!_options.Enabled)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return null;
            }

            // Basic validation to avoid calling external API for garbage input
            if (!IPAddress.TryParse(ipAddress, out _))
            {
                _logger.LogDebug("Skipping GeoIP lookup for invalid IP: {IpAddress}", ipAddress);
                return null;
            }

            var url = BuildRequestUrl(ipAddress);

            try
            {
                // For ipapi.co we can use GetFromJsonAsync
                var response = await _httpClient.GetFromJsonAsync<IpApiCoResponse>(
                    url,
                    cancellationToken);

                if (response is null)
                {
                    _logger.LogWarning("GeoIP provider returned null response for IP {IpAddress}", ipAddress);
                    return null;
                }

                if (response.Error)
                {
                    _logger.LogWarning(
                        "GeoIP provider returned error for IP {IpAddress}: {Reason}",
                        ipAddress,
                        response.Reason);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(response.CountryName))
                {
                    // No country -> не очень полезно
                    return null;
                }

                // Domain value object
                return DomainGeoLocation.Create(
                    response.CountryName,
                    response.Region,
                    response.City);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Пробрасываем отмену наверх
                throw;
            }
            catch (Exception ex)
            {
                // Любые сетевые/JSON ошибки – логируем и возвращаем null, чтобы не ронять логин/рефреш
                _logger.LogWarning(
                    ex,
                    "Failed to resolve geo location for IP {IpAddress}",
                    ipAddress);

                return null;
            }
        }

        private string BuildRequestUrl(string ipAddress)
        {
            // Простая подстановка {ip}
            return _options.EndpointTemplate.Replace("{ip}", ipAddress, StringComparison.Ordinal);
        }
    }
}
