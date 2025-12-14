using System.Net;
using System.Net.Http.Json;
using Matrix.Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainGeoLocation = Matrix.Identity.Domain.ValueObjects.GeoLocation;

namespace Matrix.Identity.Infrastructure.Integration.GeoLocation
{
    /// <summary>
    ///     HTTP-based implementation of IGeoLocationService using external GeoIP provider.
    /// </summary>
    public sealed class GeoLocationService : IGeoLocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeoLocationService> _logger;
        private readonly GeoLocationOptions _options;

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
                return null;

            if (string.IsNullOrWhiteSpace(ipAddress))
                return null;

            // Basic validation to avoid calling external API for garbage input
            if (!IPAddress.TryParse(
                    ipString: ipAddress,
                    address: out _))
            {
                _logger.LogDebug(
                    message: "Skipping GeoIP lookup for invalid IP: {IpAddress}",
                    ipAddress);
                return null;
            }

            string url = BuildRequestUrl(ipAddress);

            try
            {
                // For ipapi.co we can use GetFromJsonAsync
                IpApiCoResponse? response = await _httpClient.GetFromJsonAsync<IpApiCoResponse>(
                    requestUri: url,
                    cancellationToken: cancellationToken);

                if (response is null)
                {
                    _logger.LogWarning(
                        message: "GeoIP provider returned null response for IP {IpAddress}",
                        ipAddress);
                    return null;
                }

                if (response.Error)
                {
                    _logger.LogWarning(
                        message: "GeoIP provider returned error for IP {IpAddress}: {Reason}",
                        ipAddress,
                        response.Reason);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(response.CountryName))
                    // No country -> не очень полезно
                    return null;

                // Domain value object
                return DomainGeoLocation.Create(
                    country: response.CountryName,
                    region: response.Region,
                    city: response.City);
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
                    exception: ex,
                    message: "Failed to resolve geo location for IP {IpAddress}",
                    ipAddress);

                return null;
            }
        }

        private string BuildRequestUrl(string ipAddress)
        {
            // Простая подстановка {ip}
            return _options.EndpointTemplate.Replace(
                oldValue: "{ip}",
                newValue: ipAddress,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
