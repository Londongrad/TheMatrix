using Matrix.Education.Contracts.Events;

namespace Matrix.Education.Application.Abstractions;

public interface IEducationStudentParticipationBatchStore
{
    Task AddAsync(EducationStudentParticipationBatchV1 batch, CancellationToken cancellationToken = default);
}
