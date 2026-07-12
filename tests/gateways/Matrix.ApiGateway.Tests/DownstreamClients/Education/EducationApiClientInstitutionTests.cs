using System.Net;
using Matrix.ApiGateway.DownstreamClients.Education;
using Matrix.Education.Contracts.Institutions;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Education
{
    public sealed class EducationApiClientInstitutionTests
    {
        [Fact]
        public async Task ListInstitutionsAsync_WhenResponseIsSuccessful_GetsCatalog()
        {
            var simulationHostId = Guid.Parse("0cd851e1-5c8b-4cf3-a91f-8268809a52d2");
            var institutionId = Guid.Parse("a8425817-300d-43e3-b1b2-e48ca865b11c");
            var expected = new EducationInstitutionCatalogResponse(
                Institutions:
                [
                    new EducationInstitutionResponse(
                        InstitutionId: institutionId,
                        Name: "Central Education Complex",
                        Kind: "school",
                        LocationAnchorId: institutionId,
                        Capacity: 640,
                        CurrentEnrollmentCount: 17,
                        AvailableSeatCount: 623)
                ]);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: expected))
            };
            IEducationApiClient client = CreateEducationApiClient(CreateHttpClient(handler));

            EducationInstitutionCatalogResponse result = await client.ListInstitutionsAsync(
                simulationHostId: simulationHostId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: Assert.Single(expected.Institutions),
                actual: Assert.Single(result.Institutions));
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Get,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: $"/api/simulation-hosts/{simulationHostId:D}/education/institutions",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task SynchronizeInstitutionsAsync_WhenResponseIsSuccessful_PutsProvisioningBatch()
        {
            var simulationHostId = Guid.Parse("a75f11a7-87ad-4e71-8652-b938357c941a");
            var institutionId = Guid.Parse("c32f398d-140a-4d16-a4fd-f69dca93c196");
            var expected = new SynchronizeEducationInstitutionsResponse(
                Status: "Applied",
                AddedInstitutions: 1,
                UpdatedInstitutions: 0,
                IgnoredInstitutions: 0);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: expected))
            };
            IEducationApiClient client = CreateEducationApiClient(CreateHttpClient(handler));

            SynchronizeEducationInstitutionsResponse result = await client.SynchronizeInstitutionsAsync(
                simulationHostId: simulationHostId,
                request: new SynchronizeEducationInstitutionsRequest(
                    SourceRevision: 17,
                    SynchronizedAtUtc: new DateTimeOffset(2026, 7, 11, 5, 0, 0, TimeSpan.Zero),
                    Institutions:
                    [
                        new EducationInstitutionProvisioningItem(
                            InstitutionId: institutionId,
                            Name: "Central Technical Institute",
                            Kind: "Institute",
                            Capacity: 2400,
                            IsActive: true)
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: expected,
                actual: result);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Put,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: $"/api/simulation-hosts/{simulationHostId:D}/education/institutions",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: $"\"institutionId\":\"{institutionId:D}\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"sourceRevision\":17",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
