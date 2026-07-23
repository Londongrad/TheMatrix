using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Domain.Scenarios.ClassicCity.Attendance;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Simulation.Primitives;
using MediatR;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Attendance;

public sealed class EvaluateLearningAttendanceCommandHandler(IStudentProfileRepository profileRepository,
    IEducationSimulationRuntimeRepository runtimeRepository, IEducationSimulationDeletionRepository deletionRepository,
    IEducationAttendanceOutboxWriter outboxWriter, IEducationUnitOfWork unitOfWork,
    ClassicCityLearningAttendancePolicy policy, TimeProvider timeProvider) : IRequestHandler<EvaluateLearningAttendanceCommand, int>
{
    public Task<int> Handle(EvaluateLearningAttendanceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Residents);
        var hostId = new SimulationHostId(request.SimulationHostId);
        if (request.SourceTickId < 0 || request.ObservedAtSimTimeUtc.Offset != TimeSpan.Zero
            || request.Residents.Count is < 1 or > 1000
            || request.Residents.Any(resident => resident is null || resident.ResidentId == Guid.Empty
                || resident.LifecycleRevision < 0 || resident.ParticipationRevision <= 0 || resident.Conditions is null)
            || request.Residents.Select(resident => resident.ResidentId).Distinct().Count() != request.Residents.Count)
            throw new ArgumentException("Invalid attendance observation batch.", nameof(request));

        // Validate all facts before opening the transaction, including rows which may later be stale.
        decimal[] attendance = request.Residents.Select(resident => policy.Evaluate(resident.Conditions.ToDomain())).ToArray();
        return unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(hostId, token) is not null)
                return 0;
            await runtimeRepository.EnsureAsync(hostId,
                new SimulationRuntimeKey(new SimulationScenarioKey("classic-city"), new SimulationHostTypeKey("city")), token);
            var profiles = (await profileRepository.GetByIdsAsync(
                request.Residents.Select(resident => new ResidentId(resident.ResidentId)).ToArray(), token))
                .ToDictionary(profile => profile.ResidentId.Value);
            var results = new List<EducationAttendanceEvaluatedV1>();
            for (int index = 0; index < request.Residents.Count; index++)
            {
                var input = request.Residents[index];
                if (!profiles.TryGetValue(input.ResidentId, out var profile) || profile.SimulationHostId != hostId)
                    continue;
                if (profile.TryRecordAttendance(request.SourceTickId, input.ParticipationRevision, input.LifecycleRevision,
                    request.ObservedAtSimTimeUtc, attendance[index], input.Conditions.CommuteAccessibility))
                    results.Add(new(input.ResidentId, input.LifecycleRevision, input.ParticipationRevision,
                        attendance[index], input.Conditions.CommuteAccessibility));
            }
            if (results.Count > 0)
                await outboxWriter.AddAsync(new(request.SimulationHostId, request.SourceTickId,
                    request.ObservedAtSimTimeUtc, timeProvider.GetUtcNow(), results), token);
            await unitOfWork.SaveChangesAsync(token);
            return results.Count;
        }, cancellationToken, IsolationLevel.Serializable);
    }
}
