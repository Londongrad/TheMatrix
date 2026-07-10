using Matrix.Education.Api.Controllers;
using Matrix.Education.Api.Tests.TestSupport;
using Matrix.Education.Application.Enrollments.CompleteStudentStage;
using Matrix.Education.Application.Enrollments.EnrollStudent;
using Matrix.Education.Application.Enrollments.WithdrawStudent;
using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.Education.Contracts.Enrollments;
using Matrix.Education.Contracts.Institutions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Matrix.Education.Api.Tests.Controllers
{
    public sealed class EducationOwnershipControllersTests
    {
        [Fact]
        public async Task Synchronize_MapsProvisioningBatchAndResponse()
        {
            var sender = new EducationApiSenderStub();
            sender.Handle<SynchronizeEducationInstitutionsCommand, SynchronizeEducationInstitutionsResult>(
                _ => new SynchronizeEducationInstitutionsResult(
                    SynchronizeEducationInstitutionsStatus.Applied,
                    1,
                    2,
                    3));
            var controller = new EducationInstitutionsController(sender);
            Guid simulationHostId = Guid.NewGuid();
            Guid institutionId = Guid.NewGuid();
            var synchronizedAtUtc = new DateTimeOffset(2048, 5, 1, 10, 0, 0, TimeSpan.Zero);

            ActionResult<SynchronizeEducationInstitutionsResponse> action = await controller.Synchronize(
                simulationHostId,
                new SynchronizeEducationInstitutionsRequest(
                    SourceRevision: 17,
                    SynchronizedAtUtc: synchronizedAtUtc,
                    Institutions:
                    [
                        new EducationInstitutionProvisioningItem(
                            institutionId,
                            "Central school",
                            "school",
                            120,
                            true)
                    ]));

            var response = Assert.IsType<SynchronizeEducationInstitutionsResponse>(
                Assert.IsType<OkObjectResult>(action.Result).Value);
            SynchronizeEducationInstitutionsCommand command =
                Assert.IsType<SynchronizeEducationInstitutionsCommand>(Assert.Single(sender.Requests));
            Assert.Equal(simulationHostId, command.SimulationHostId);
            Assert.Equal(17, command.SourceRevision);
            Assert.Equal(institutionId, Assert.Single(command.Institutions).InstitutionId);
            Assert.Equal("Applied", response.Status);
            Assert.Equal(1, response.AddedInstitutions);
        }

        [Fact]
        public async Task EnrollmentOperations_MapRequestsAndResponses()
        {
            var sender = new EducationApiSenderStub();
            Guid enrollmentId = Guid.NewGuid();
            sender.Handle<EnrollStudentCommand, EnrollStudentResult>(
                _ => new EnrollStudentResult(EnrollStudentStatus.Applied, enrollmentId));
            sender.Handle<CompleteStudentStageCommand, CompleteStudentStageResult>(
                _ => new CompleteStudentStageResult(
                    CompleteStudentStageStatus.Applied,
                    enrollmentId,
                    "upper-secondary"));
            sender.Handle<WithdrawStudentCommand, WithdrawStudentResult>(
                _ => new WithdrawStudentResult(WithdrawStudentStatus.Applied, enrollmentId));
            var controller = new EducationEnrollmentsController(sender);
            Guid simulationHostId = Guid.NewGuid();
            Guid residentId = Guid.NewGuid();
            Guid institutionId = Guid.NewGuid();

            ActionResult<EducationEnrollmentOperationResponse> enrolled = await controller.Enroll(
                simulationHostId,
                new EnrollStudentRequest(
                    residentId,
                    institutionId,
                    "upper-secondary",
                    new DateOnly(2048, 5, 1)));
            ActionResult<EducationEnrollmentOperationResponse> completed = await controller.Complete(
                simulationHostId,
                new CompleteStudentStageRequest(residentId, new DateOnly(2048, 6, 30)));
            ActionResult<EducationEnrollmentOperationResponse> withdrawn = await controller.Withdraw(
                simulationHostId,
                new WithdrawStudentRequest(residentId, new DateOnly(2048, 5, 4)));

            Assert.Equal(enrollmentId, Read(enrolled).EnrollmentId);
            Assert.Equal("upper-secondary", Read(completed).CompletedStage);
            Assert.Equal("Applied", Read(withdrawn).Status);
            Assert.Collection(
                sender.Requests,
                request => Assert.IsType<EnrollStudentCommand>(request),
                request => Assert.IsType<CompleteStudentStageCommand>(request),
                request => Assert.IsType<WithdrawStudentCommand>(request));
        }

        private static EducationEnrollmentOperationResponse Read(
            ActionResult<EducationEnrollmentOperationResponse> action)
        {
            return Assert.IsType<EducationEnrollmentOperationResponse>(
                Assert.IsType<OkObjectResult>(action.Result).Value);
        }
    }
}
