using Matrix.Education.Contracts.Events;

namespace Matrix.Education.Application.Abstractions;

public interface IEducationAttendanceOutboxWriter
{
    Task AddAsync(EducationAttendanceEvaluatedBatchV1 batch, CancellationToken cancellationToken);
}
