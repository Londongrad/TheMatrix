using Matrix.Education.Application.Enrollments.CompleteStudentStage;
using Matrix.Education.Application.Enrollments.EnrollStudent;
using Matrix.Education.Application.Enrollments.WithdrawStudent;
using Matrix.Education.Contracts;
using Matrix.Education.Contracts.Enrollments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Education.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route(EducationApiRoutes.Enrollments)]
    public sealed class EducationEnrollmentsController(ISender sender) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<EducationEnrollmentOperationResponse>> Enroll(
            [FromRoute] Guid simulationHostId,
            [FromBody] EnrollStudentRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            EnrollStudentResult result = await sender.Send(
                new EnrollStudentCommand(
                    SimulationHostId: simulationHostId,
                    ResidentId: request.ResidentId,
                    InstitutionId: request.InstitutionId,
                    Stage: request.Stage,
                    EnrolledOn: request.EnrolledOn),
                cancellationToken);

            return Ok(new EducationEnrollmentOperationResponse(
                Status: result.Status.ToString(),
                EnrollmentId: result.EnrollmentId));
        }

        [HttpPost("complete")]
        public async Task<ActionResult<EducationEnrollmentOperationResponse>> Complete(
            [FromRoute] Guid simulationHostId,
            [FromBody] CompleteStudentStageRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            CompleteStudentStageResult result = await sender.Send(
                new CompleteStudentStageCommand(
                    SimulationHostId: simulationHostId,
                    ResidentId: request.ResidentId,
                    CompletedOn: request.CompletedOn),
                cancellationToken);

            return Ok(new EducationEnrollmentOperationResponse(
                Status: result.Status.ToString(),
                EnrollmentId: result.EnrollmentId,
                CompletedStage: result.CompletedStage));
        }

        [HttpPost("withdraw")]
        public async Task<ActionResult<EducationEnrollmentOperationResponse>> Withdraw(
            [FromRoute] Guid simulationHostId,
            [FromBody] WithdrawStudentRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            WithdrawStudentResult result = await sender.Send(
                new WithdrawStudentCommand(
                    SimulationHostId: simulationHostId,
                    ResidentId: request.ResidentId,
                    WithdrawnOn: request.WithdrawnOn),
                cancellationToken);

            return Ok(new EducationEnrollmentOperationResponse(
                Status: result.Status.ToString(),
                EnrollmentId: result.EnrollmentId));
        }
    }
}
