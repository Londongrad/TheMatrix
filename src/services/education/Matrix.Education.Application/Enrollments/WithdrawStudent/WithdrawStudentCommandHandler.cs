using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Integration;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using MediatR;

namespace Matrix.Education.Application.Enrollments.WithdrawStudent
{
    public sealed class WithdrawStudentCommandHandler(
        IStudentProfileRepository studentProfileRepository,
        IEducationInstitutionRepository institutionRepository,
        IStudentEnrollmentRepository enrollmentRepository,
        IEducationSimulationDeletionRepository deletionRepository,
        IEducationStudentParticipationOutboxWriter participationOutboxWriter,
        IEducationUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<WithdrawStudentCommand, WithdrawStudentResult>
    {
        public Task<WithdrawStudentResult> Handle(
            WithdrawStudentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var residentId = new ResidentId(request.ResidentId);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => WithdrawInsideTransactionAsync(
                    simulationHostId,
                    residentId,
                    request.WithdrawnOn,
                    token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<WithdrawStudentResult> WithdrawInsideTransactionAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            DateOnly withdrawnOn,
            CancellationToken cancellationToken)
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(
                    simulationHostId,
                    cancellationToken) is not null)
                return Result(WithdrawStudentStatus.SimulationDeleted);

            StudentEnrollment? enrollment = await enrollmentRepository.GetActiveByResidentAsync(
                simulationHostId,
                residentId,
                cancellationToken);
            if (enrollment is null)
                return Result(WithdrawStudentStatus.NotEnrolled);

            StudentProfile student = (await studentProfileRepository.GetByIdsAsync(
                                         [residentId],
                                         cancellationToken))
                                    .SingleOrDefault()
                                ?? throw new InvalidOperationException(
                                    "An active enrollment must reference an existing student profile.");

            EducationInstitution institution = await institutionRepository.GetAsync(
                                                   simulationHostId,
                                                   enrollment.InstitutionId,
                                                   cancellationToken)
                                               ?? throw new InvalidOperationException(
                                                   "An active enrollment must reference an existing institution.");

            enrollment.Withdraw(withdrawnOn);
            institution.ReleaseSeats(1);
            student.RecordParticipationChange();
            await participationOutboxWriter.AddChangesAsync(
                simulationHostId: simulationHostId.Value,
                snapshotDate: withdrawnOn,
                occurredAtUtc: timeProvider.GetUtcNow(),
                correlationId: $"education:enrollment:{enrollment.EnrollmentId.Value}:withdrawn",
                changes: [EducationStudentParticipationChange.Capture(student)],
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new WithdrawStudentResult(
                Status: WithdrawStudentStatus.Applied,
                EnrollmentId: enrollment.EnrollmentId.Value);
        }

        private static WithdrawStudentResult Result(WithdrawStudentStatus status) => new(status);
    }
}
