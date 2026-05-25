using System.Net;
using Matrix.Identity.Infrastructure.Integration.GeoLocation;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Integration.GeoLocation
{
    public sealed class GeoLocationServiceTests
    {
        [Fact]
        public async Task ResolveAsync_WhenDisabled_ReturnsNullWithoutCallingHttp()
        {
            int requestCount = 0;
            var service = new GeoLocationService(
                httpClient: CreateHttpClient((
                    _,
                    _) =>
                {
                    requestCount++;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }),
                options: CreateGeoLocationOptions(enabled: false),
                logger: new TestLogger<GeoLocationService>());

            Domain.ValueObjects.GeoLocation? result = await service.ResolveAsync(
                ipAddress: "127.0.0.1",
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: 0,
                actual: requestCount);
        }

        [Fact]
        public async Task ResolveAsync_WhenIpIsInvalid_ReturnsNullWithoutCallingHttp()
        {
            int requestCount = 0;
            var service = new GeoLocationService(
                httpClient: CreateHttpClient((
                    _,
                    _) =>
                {
                    requestCount++;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }),
                options: CreateGeoLocationOptions(),
                logger: new TestLogger<GeoLocationService>());

            Domain.ValueObjects.GeoLocation? result = await service.ResolveAsync(
                ipAddress: "not-an-ip",
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: 0,
                actual: requestCount);
        }

        [Fact]
        public async Task ResolveAsync_WhenProviderReturnsCountry_MapsLocation()
        {
            HttpRequestMessage? capturedRequest = null;
            var service = new GeoLocationService(
                httpClient: CreateHttpClient((
                    request,
                    _) =>
                {
                    capturedRequest = request;
                    return Task.FromResult(
                        JsonResponse(
                            new
                            {
                                country_name = "Russia",
                                region = "Zabaykalsky Krai",
                                city = "Chita",
                                error = false
                            }));
                }),
                options: CreateGeoLocationOptions(endpointTemplate: "https://ipapi.co/{ip}/json/"),
                logger: new TestLogger<GeoLocationService>());

            Domain.ValueObjects.GeoLocation? result = await service.ResolveAsync(
                ipAddress: "127.0.0.1",
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: "Russia",
                actual: result.Country);
            Assert.Equal(
                expected: "Chita",
                actual: result.City);
            Assert.Equal(
                expected: "https://ipapi.co/127.0.0.1/json/",
                actual: capturedRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task ResolveAsync_WhenJsonIsMalformed_ReturnsNullAndLogsWarning()
        {
            var logger = new TestLogger<GeoLocationService>();
            var service = new GeoLocationService(
                httpClient: CreateHttpClient((
                    _,
                    _) =>
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("{")
                        });
                }),
                options: CreateGeoLocationOptions(),
                logger: logger);

            Domain.ValueObjects.GeoLocation? result = await service.ResolveAsync(
                ipAddress: "127.0.0.1",
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Contains(
                collection: logger.Entries,
                filter: x => x.LogLevel == LogLevel.Warning);
        }
    }
}
