using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories;

public sealed class EducationAttendanceProjectionWriter(PopulationDbContext dbContext) : IEducationAttendanceProjectionWriter
{
    public async Task<int> ApplyAsync(Guid simulationHostId, long sourceTickId, DateTimeOffset observedAtSimTimeUtc,
        IReadOnlyCollection<EducationAttendanceInput> residents, CancellationToken cancellationToken)
    {
        var ids = residents.Select(resident => PersonId.From(resident.ResidentId)).ToArray();
        var projections = await dbContext.EducationParticipationProjections
            .Where(projection => projection.SimulationHostId == simulationHostId && ids.Contains(projection.ResidentId))
            .ToDictionaryAsync(projection => projection.ResidentId.Value, cancellationToken);
        int applied = 0;
        foreach (var input in residents)
            if (projections.TryGetValue(input.ResidentId, out var projection)
                && projection.TryApplyAttendance(sourceTickId, observedAtSimTimeUtc, input))
                applied++;
        return applied;
    }
}
