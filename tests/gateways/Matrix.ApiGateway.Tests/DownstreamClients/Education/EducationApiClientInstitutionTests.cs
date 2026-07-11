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
