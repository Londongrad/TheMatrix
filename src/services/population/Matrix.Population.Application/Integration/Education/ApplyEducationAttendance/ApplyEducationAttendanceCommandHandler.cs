using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Integration.Education.ApplyEducationAttendance;

public sealed class ApplyEducationAttendanceCommandHandler(IPersonReadRepository personRepository,
    IEducationAttendanceProjectionWriter projectionWriter, IUnitOfWork unitOfWork)
    : IRequestHandler<ApplyEducationAttendanceCommand, int>
{
    public Task<int> Handle(ApplyEducationAttendanceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SimulationHostId == Guid.Empty || request.SourceTickId < 0
            || request.ObservedAtSimTimeUtc.Offset != TimeSpan.Zero || request.Residents is null
            || request.Residents.Count is < 1 or > 1000
            || request.Residents.Any(resident => resident is null || resident.ResidentId == Guid.Empty
                || resident.ResidentLifecycleRevision < 0 || resident.ParticipationRevision <= 0
                || resident.AttendanceIndex is < 0m or > 1m || resident.CommuteAccessibilityIndex is < 0m or > 2m)
            || request.Residents.Select(resident => resident.ResidentId).Distinct().Count() != request.Residents.Count)
            throw new ArgumentException("Invalid education attendance batch.", nameof(request));

        return unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var residents = (await personRepository.GetByIdsAsync(
                request.Residents.Select(resident => PersonId.From(resident.ResidentId)).ToArray(), token))
                .ToDictionary(resident => resident.Id.Value);
            var current = request.Residents.Where(input => residents.TryGetValue(input.ResidentId, out var resident)
                && resident.IsAlive && resident.LifecycleRevision == input.ResidentLifecycleRevision).ToArray();
            int applied = current.Length == 0 ? 0 : await projectionWriter.ApplyAsync(request.SimulationHostId,
                request.SourceTickId, request.ObservedAtSimTimeUtc, current, token);
            await unitOfWork.SaveChangesAsync(token);
            return applied;
        }, cancellationToken, IsolationLevel.Serializable);
    }
}
