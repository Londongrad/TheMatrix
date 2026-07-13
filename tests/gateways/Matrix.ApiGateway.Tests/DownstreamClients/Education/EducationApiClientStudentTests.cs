using System.Net;
using Matrix.ApiGateway.DownstreamClients.Education;
using Matrix.Education.Contracts.Students;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Education;

public sealed class EducationApiClientStudentTests
{
    [Fact]
    public async Task GetStudentStatusAsync_WhenProfileExists_GetsEducationState()
    {
        Guid simulationHostId = Guid.NewGuid();
        Guid residentId = Guid.NewGuid();
        var expected = new StudentEducationStatusResponse(
            ResidentId: residentId,
            IsAlive: true,
            IsActive: true,
            CompletedStage: "primary",
            CompletedStageOn: new DateOnly(2047, 6, 30),
            ActiveEnrollment: new ActiveStudentEnrollmentResponse(
                EnrollmentId: Guid.NewGuid(),
                InstitutionId: Guid.NewGuid(),
                InstitutionName: "Central School",
                InstitutionKind: "school",
                LocationAnchorId: Guid.NewGuid(),
                Stage: "secondary",
                EnrolledOn: new DateOnly(2048, 5, 1)));
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                CreateJsonResponse(HttpStatusCode.OK, expected))
        };
        IEducationApiClient client = CreateEducationApiClient(CreateHttpClient(handler));

        StudentEducationStatusResponse? result = await client.GetStudentStatusAsync(
            simulationHostId,
            residentId,
            CancellationToken.None);

        Assert.Equal(expected, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith(
            $"/api/simulation-hosts/{simulationHostId:D}/education/students/{residentId:D}",
            request.RequestUri,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStudentStatusAsync_WhenProfileIsUnknown_ReturnsNull()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.NotFound))
        };
        IEducationApiClient client = CreateEducationApiClient(CreateHttpClient(handler));

        StudentEducationStatusResponse? result = await client.GetStudentStatusAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }
}
