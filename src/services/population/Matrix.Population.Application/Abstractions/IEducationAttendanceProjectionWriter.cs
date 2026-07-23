using Matrix.Population.Application.Integration.Education;

namespace Matrix.Population.Application.Abstractions;

public interface IEducationAttendanceProjectionWriter
{
    Task<int> ApplyAsync(Guid simulationHostId, long sourceTickId, DateTimeOffset observedAtSimTimeUtc,
        IReadOnlyCollection<EducationAttendanceInput> residents, CancellationToken cancellationToken);
}
