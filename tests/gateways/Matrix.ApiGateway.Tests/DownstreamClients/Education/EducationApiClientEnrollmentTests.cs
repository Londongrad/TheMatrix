using System.Net;
using Matrix.ApiGateway.DownstreamClients.Education;
using Matrix.Education.Contracts.Enrollments;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Education
{
    public sealed class EducationApiClientEnrollmentTests
    {
        private static readonly Guid SimulationHostId =
            Guid.Parse("dffbb638-f0ac-4097-b511-4551249c082b");

        private static readonly Guid ResidentId =
            Guid.Parse("028484c6-158a-4a17-906d-a15ba2007fb3");

        [Fact]
        public async Task EnrollStudentAsync_WhenResponseIsSuccessful_PostsEnrollment()
        {
            var institutionId = Guid.Parse("eb87752a-b050-4f58-b917-31fb021be0fd");
            var enrollmentId = Guid.Parse("fc6e9fa0-3ae8-4ffd-9528-5a3a280b5db7");
            var handler = CreateSuccessfulHandler(
                new EducationEnrollmentOperationResponse(
                    Status: "Enrolled",
                    EnrollmentId: enrollmentId));
            IEducationApiClient client = CreateEducationApiClient(CreateHttpClient(handler));

            EducationEnrollmentOperationResponse result = await client.EnrollStudentAsync(
                simulationHostId: SimulationHostId,
                request: new EnrollStudentRequest(
                    ResidentId: ResidentId,
                    InstitutionId: institutionId,
                    Stage: "Higher",
                    EnrolledOn: new DateOnly(2026, 7, 11)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: enrollmentId,
                actual: result.EnrollmentId);
            RecordedRequest request = Assert.Single(handler.Requests);
            AssertEducationEnrollmentRequest(
                request: request,
                expectedPathSuffix: string.Empty);
            Assert.Contains(
                expectedSubstring: $"\"institutionId\":\"{institutionId:D}\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"stage\":\"Higher\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task CompleteStudentStageAsync_WhenResponseIsSuccessful_PostsCompletion()
        {
            var enrollmentId = Guid.Parse("727a2820-f9d4-451c-a654-253177089a28");
            var handler = CreateSuccessfulHandler(
                new EducationEnrollmentOperationResponse(
                    Status: "Completed",
                    EnrollmentId: enrollmentId,
                    CompletedStage: "Higher"));
            IEducationApiClient client = CreateEducationApiClient(CreateHttpClient(handler));

            EducationEnrollmentOperationResponse result = await client.CompleteStudentStageAsync(
                simulationHostId: SimulationHostId,
                request: new CompleteStudentStageRequest(
                    ResidentId: ResidentId,
                    CompletedOn: new DateOnly(2026, 7, 11)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Higher",
                actual: result.CompletedStage);
            AssertEducationEnrollmentRequest(
                request: Assert.Single(handler.Requests),
                expectedPathSuffix: "/complete");
        }

        [Fact]
        public async Task WithdrawStudentAsync_WhenResponseIsSuccessful_PostsWithdrawal()
        {
            var enrollmentId = Guid.Parse("2be8fd44-d28e-4326-b8fd-bb95447e19ef");
            var handler = CreateSuccessfulHandler(
                new EducationEnrollmentOperationResponse(
                    Status: "Withdrawn",
                    EnrollmentId: enrollmentId));
            IEducationApiClient client = CreateEducationApiClient(CreateHttpClient(handler));

            EducationEnrollmentOperationResponse result = await client.WithdrawStudentAsync(
                simulationHostId: SimulationHostId,
                request: new WithdrawStudentRequest(
                    ResidentId: ResidentId,
                    WithdrawnOn: new DateOnly(2026, 7, 11)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: enrollmentId,
                actual: result.EnrollmentId);
            AssertEducationEnrollmentRequest(
                request: Assert.Single(handler.Requests),
                expectedPathSuffix: "/withdraw");
        }

        private static RecordingHttpMessageHandler CreateSuccessfulHandler(
            EducationEnrollmentOperationResponse response)
        {
            return new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: response))
            };
        }

        private static void AssertEducationEnrollmentRequest(
            RecordedRequest request,
            string expectedPathSuffix)
        {
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString:
                $"/api/simulation-hosts/{SimulationHostId:D}/education/enrollments{expectedPathSuffix}",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: $"\"residentId\":\"{ResidentId:D}\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
