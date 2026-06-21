using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Domain.Enrollments
{
    public sealed class StudentEnrollment : AggregateRoot<EnrollmentId>
    {
        private StudentEnrollment(
            EnrollmentId id,
            SimulationHostId simulationHostId,
            ResidentId residentId,
            EducationInstitutionId institutionId,
            EducationStageKey stage,
            DateOnly enrolledOn,
            EnrollmentStatus status,
            DateOnly? closedOn)
            : base(id)
        {
            SimulationHostId = simulationHostId;
            ResidentId = residentId;
            InstitutionId = institutionId;
            Stage = stage;
            EnrolledOn = enrolledOn;
            Status = status;
            ClosedOn = closedOn;
        }

        private StudentEnrollment()
            : base(default(EnrollmentId))
        {
        }

        public EnrollmentId EnrollmentId => Id;
        public SimulationHostId SimulationHostId { get; private set; }
        public ResidentId ResidentId { get; private set; }
        public EducationInstitutionId InstitutionId { get; private set; }
        public EducationStageKey Stage { get; private set; }
        public DateOnly EnrolledOn { get; private set; }
        public EnrollmentStatus Status { get; private set; }
        public DateOnly? ClosedOn { get; private set; }
        public bool IsActive => Status == EnrollmentStatus.Active;

        public static StudentEnrollment Enroll(
            EnrollmentId id,
            SimulationHostId simulationHostId,
            ResidentId residentId,
            EducationInstitutionId institutionId,
            EducationStageKey stage,
            DateOnly enrolledOn)
        {
            return new StudentEnrollment(
                id: id,
                simulationHostId: simulationHostId,
                residentId: residentId,
                institutionId: institutionId,
                stage: stage,
                enrolledOn: enrolledOn,
                status: EnrollmentStatus.Active,
                closedOn: null);
        }

        public bool Complete(DateOnly completedOn)
        {
            return Close(
                targetStatus: EnrollmentStatus.Completed,
                closedOn: completedOn);
        }

        public bool Withdraw(DateOnly withdrawnOn)
        {
            return Close(
                targetStatus: EnrollmentStatus.Withdrawn,
                closedOn: withdrawnOn);
        }

        private bool Close(EnrollmentStatus targetStatus, DateOnly closedOn)
        {
            if (Status == targetStatus)
                return false;

            if (!IsActive)
                throw new InvalidOperationException(
                    $"A {Status.ToString().ToLowerInvariant()} enrollment cannot become {targetStatus.ToString().ToLowerInvariant()}.");

            if (closedOn < EnrolledOn)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(closedOn),
                    message: "Enrollment cannot close before its enrollment date.");

            Status = targetStatus;
            ClosedOn = closedOn;

            return true;
        }
    }
}
