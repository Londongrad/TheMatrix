using Matrix.Education.Contracts.Events;

namespace Matrix.Education.Application.Abstractions
{
    public interface IEducationStudentParticipationOutboxWriter
    {
        Task AddAsync(
            EducationStudentParticipationBatchV1 batch,
            CancellationToken cancellationToken = default);
    }
}
