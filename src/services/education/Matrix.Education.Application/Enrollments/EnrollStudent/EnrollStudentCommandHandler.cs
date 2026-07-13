using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using MediatR;

namespace Matrix.Education.Application.Enrollments.EnrollStudent
{
    public sealed class EnrollStudentCommandHandler(
        IStudentProfileRepository studentProfileRepository,
        IEducationInstitutionRepository institutionRepository,
        IStudentEnrollmentRepository enrollmentRepository,
        IEducationSimulationDeletionRepository deletionRepository,
        IEducationUnitOfWork unitOfWork)
        : IRequestHandler<EnrollStudentCommand, EnrollStudentResult>
    {
        public Task<EnrollStudentResult> Handle(
            EnrollStudentCommand request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var residentId = new ResidentId(request.ResidentId);
            var institutionId = new EducationInstitutionId(request.InstitutionId);
            var stage = new EducationStageKey(request.Stage);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => EnrollInsideTransactionAsync(
                    simulationHostId,
                    residentId,
                    institutionId,
                    stage,
                    request.EnrolledOn,
                    token),
                cancellationToken: cancellationToken,
                isolationLevel: IsolationLevel.Serializable);
        }

        private async Task<EnrollStudentResult> EnrollInsideTransactionAsync(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            EducationInstitutionId institutionId,
            EducationStageKey stage,
            DateOnly enrolledOn,
            CancellationToken cancellationToken)
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(
                    simulationHostId,
                    cancellationToken) is not null)
                return Result(EnrollStudentStatus.SimulationDeleted);

            StudentProfile? student = (await studentProfileRepository.GetByIdsAsync(
                    [residentId],
                    cancellationToken))
               .SingleOrDefault();
            if (student is null || student.SimulationHostId != simulationHostId)
                return Result(EnrollStudentStatus.StudentNotFound);
            if (!student.IsAlive || !student.IsActive)
                return Result(EnrollStudentStatus.StudentUnavailable);

            StudentEnrollment? activeEnrollment = await enrollmentRepository.GetActiveByResidentAsync(
                simulationHostId,
                residentId,
                cancellationToken);
            if (activeEnrollment is not null)
                return new EnrollStudentResult(
                    Status: EnrollStudentStatus.AlreadyEnrolled,
                    EnrollmentId: activeEnrollment.EnrollmentId.Value);

            EducationInstitution? institution = await institutionRepository.GetAsync(
                simulationHostId,
                institutionId,
                cancellationToken);
            if (institution is null)
                return Result(EnrollStudentStatus.InstitutionNotFound);
            if (!institution.IsActive)
                return Result(EnrollStudentStatus.InstitutionInactive);
            if (!institution.TryReserveSeats(1))
                return Result(EnrollStudentStatus.CapacityUnavailable);

            StudentEnrollment enrollment = StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: simulationHostId,
                residentId: residentId,
                institutionId: institutionId,
                stage: stage,
                enrolledOn: enrolledOn);
            await enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new EnrollStudentResult(
                Status: EnrollStudentStatus.Applied,
                EnrollmentId: enrollment.EnrollmentId.Value);
        }

        private static EnrollStudentResult Result(EnrollStudentStatus status) => new(status);
    }
}
