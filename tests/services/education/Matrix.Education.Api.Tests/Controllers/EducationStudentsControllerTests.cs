using Matrix.Education.Api.Controllers;
using Matrix.Education.Api.Tests.TestSupport;
using Matrix.Education.Application.Students.GetStudentEducationStatus;
using Matrix.Education.Contracts.Students;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Matrix.Education.Api.Tests.Controllers;

public sealed class EducationStudentsControllerTests
{
    [Fact]
    public async Task GetStatus_MapsEducationOwnedStudentState()
    {
        Guid simulationHostId = Guid.NewGuid();
        Guid residentId = Guid.NewGuid();
        Guid enrollmentId = Guid.NewGuid();
        Guid institutionId = Guid.NewGuid();
        Guid locationAnchorId = Guid.NewGuid();
        var sender = new EducationApiSenderStub();
        sender.Handle<GetStudentEducationStatusQuery, StudentEducationStatusView?>(
            _ => new StudentEducationStatusView(
                ResidentId: residentId,
                IsAlive: true,
                IsActive: true,
                CompletedStage: "primary",
                CompletedStageOn: new DateOnly(2047, 6, 30),
                ActiveEnrollment: new ActiveStudentEnrollmentView(
                    EnrollmentId: enrollmentId,
                    InstitutionId: institutionId,
                    InstitutionName: "Central School",
                    InstitutionKind: "school",
                    LocationAnchorId: locationAnchorId,
                    Stage: "secondary",
                    EnrolledOn: new DateOnly(2048, 5, 1))));
        var controller = new EducationStudentsController(sender);

        ActionResult<StudentEducationStatusResponse> action = await controller.GetStatus(
            simulationHostId,
            residentId,
            CancellationToken.None);

        var response = Assert.IsType<StudentEducationStatusResponse>(
            Assert.IsType<OkObjectResult>(action.Result).Value);
        GetStudentEducationStatusQuery query =
            Assert.IsType<GetStudentEducationStatusQuery>(Assert.Single(sender.Requests));
        Assert.Equal(simulationHostId, query.SimulationHostId);
        Assert.Equal(residentId, query.ResidentId);
        Assert.Equal("primary", response.CompletedStage);
        Assert.NotNull(response.ActiveEnrollment);
        Assert.Equal(enrollmentId, response.ActiveEnrollment.EnrollmentId);
        Assert.Equal(institutionId, response.ActiveEnrollment.InstitutionId);
        Assert.Equal(locationAnchorId, response.ActiveEnrollment.LocationAnchorId);
        Assert.Equal("secondary", response.ActiveEnrollment.Stage);
    }

    [Fact]
    public async Task GetStatus_WhenProfileIsUnknown_ReturnsNotFound()
    {
        var sender = new EducationApiSenderStub();
        sender.Handle<GetStudentEducationStatusQuery, StudentEducationStatusView?>(_ => null);
        var controller = new EducationStudentsController(sender);

        ActionResult<StudentEducationStatusResponse> action = await controller.GetStatus(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(action.Result);
    }
}
