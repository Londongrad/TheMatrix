using Matrix.Education.Contracts.Enrollments;
using Matrix.Education.Contracts.Institutions;

namespace Matrix.ApiGateway.DownstreamClients.Education
{
    public interface IEducationApiClient
    {
        Task<SynchronizeEducationInstitutionsResponse> SynchronizeInstitutionsAsync(
            Guid simulationHostId,
            SynchronizeEducationInstitutionsRequest request,
            CancellationToken cancellationToken = default);

        Task<EducationEnrollmentOperationResponse> EnrollStudentAsync(
            Guid simulationHostId,
            EnrollStudentRequest request,
            CancellationToken cancellationToken = default);

        Task<EducationEnrollmentOperationResponse> CompleteStudentStageAsync(
            Guid simulationHostId,
            CompleteStudentStageRequest request,
            CancellationToken cancellationToken = default);

        Task<EducationEnrollmentOperationResponse> WithdrawStudentAsync(
            Guid simulationHostId,
            WithdrawStudentRequest request,
            CancellationToken cancellationToken = default);
    }
}
