using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using MediatR;

namespace Matrix.Education.Application.Enrollments.CompleteStudentStage
{
    public sealed class CompleteStudentStageCommandHandler(
        IStudentProfileRepository studentProfileRepository,
        IEducationInstitutionRepository institutionRepository,
        IStudentEnrollmentRepository enrollmentRepository,
        IEducationSimulationDeletionRepository deletionRepository,
        IEducationUnitOfWork unitOfWork)
        : IRequestHandler<CompleteStudentStageCommand, CompleteStudentStageResult>
    {
        public Task<CompleteStudentStageResult> Handle(
            CompleteStudentStageCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var residentId = new ResidentId(request.ResidentId);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => CompleteInsideTransactionAsync(
                    simulationHostId,
                    residentId,
                    request.CompletedOn,
                    token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<CompleteStudentStageResult> CompleteInsideTransactionAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            DateOnly completedOn,
            CancellationToken cancellationToken)
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(
                    simulationHostId,
                    cancellationToken) is not null)
                return Result(CompleteStudentStageStatus.SimulationDeleted);

            StudentEnrollment? enrollment = await enrollmentRepository.GetActiveByResidentAsync(
                simulationHostId,
                residentId,
                cancellationToken);
            if (enrollment is null)
                return Result(CompleteStudentStageStatus.NotEnrolled);

            StudentProfile student = (await studentProfileRepository.GetByIdsAsync(
                                         [residentId],
                                         cancellationToken))
                                    .SingleOrDefault()
                                ?? throw new InvalidOperationException(
                                    "An active enrollment must reference an existing student profile.");
            if (!student.IsAlive || !student.IsActive)
                return Result(CompleteStudentStageStatus.StudentUnavailable);

            EducationInstitution institution = await institutionRepository.GetAsync(
                                                   simulationHostId,
                                                   enrollment.InstitutionId,
                                                   cancellationToken)
                                               ?? throw new InvalidOperationException(
                                                   "An active enrollment must reference an existing institution.");

            enrollment.Complete(completedOn);
            student.RecordStageCompletion(enrollment.Stage, completedOn);
            institution.ReleaseSeats(1);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CompleteStudentStageResult(
                Status: CompleteStudentStageStatus.Applied,
                EnrollmentId: enrollment.EnrollmentId.Value,
                CompletedStage: enrollment.Stage.Value);
        }

        private static CompleteStudentStageResult Result(CompleteStudentStageStatus status) => new(status);
    }
}
