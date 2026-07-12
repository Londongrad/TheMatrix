using Matrix.Education.Application.Students.GetStudentEducationStatus;
using Matrix.Education.Contracts;
using Matrix.Education.Contracts.Students;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Education.Api.Controllers;

[ApiController]
[Authorize]
[Route(EducationApiRoutes.Students)]
public sealed class EducationStudentsController(ISender sender) : ControllerBase
{
    [HttpGet("{residentId:guid}")]
    public async Task<ActionResult<StudentEducationStatusResponse>> GetStatus(
        [FromRoute] Guid simulationHostId,
        [FromRoute] Guid residentId,
        CancellationToken cancellationToken = default)
    {
        StudentEducationStatusView? status = await sender.Send(
            new GetStudentEducationStatusQuery(simulationHostId, residentId),
            cancellationToken);

        if (status is null)
            return NotFound();

        ActiveStudentEnrollmentResponse? activeEnrollment = status.ActiveEnrollment is null
            ? null
            : new ActiveStudentEnrollmentResponse(
                EnrollmentId: status.ActiveEnrollment.EnrollmentId,
                InstitutionId: status.ActiveEnrollment.InstitutionId,
                InstitutionName: status.ActiveEnrollment.InstitutionName,
                InstitutionKind: status.ActiveEnrollment.InstitutionKind,
                LocationAnchorId: status.ActiveEnrollment.LocationAnchorId,
                Stage: status.ActiveEnrollment.Stage,
                EnrolledOn: status.ActiveEnrollment.EnrolledOn);

        return Ok(new StudentEducationStatusResponse(
            ResidentId: status.ResidentId,
            IsAlive: status.IsAlive,
            IsActive: status.IsActive,
            CompletedStage: status.CompletedStage,
            CompletedStageOn: status.CompletedStageOn,
            ActiveEnrollment: activeEnrollment));
    }
}
