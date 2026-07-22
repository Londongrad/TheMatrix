using Matrix.Education.Application.Abstractions;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Application.Integration;

public sealed class EducationStudentParticipationPublisher(
    IEducationSimulationRuntimeRepository runtimeRepository,
    EducationEconomicPolicyRegistry policyRegistry,
    IEducationStudentParticipationBatchStore batchStore) : IEducationStudentParticipationOutboxWriter
{
    private readonly Dictionary<SimulationHostId, IEducationParticipationEconomicPolicy> _policiesByHost = [];

    public async Task AddAsync(EducationStudentParticipationBatchV1 batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(batch.Students);
        cancellationToken.ThrowIfCancellationRequested();
        if (batch.Students.Count is < 1 or > EducationStudentParticipationBatchFactory.DefaultBatchSize)
            throw new ArgumentException("Participation batches must contain between 1 and 1000 students.", nameof(batch));

        var hostId = new SimulationHostId(batch.SimulationHostId);
        if (!_policiesByHost.TryGetValue(hostId, out var policy))
        {
            var runtime = await runtimeRepository.GetAsync(hostId, cancellationToken)
                ?? throw new InvalidOperationException($"Education runtime for simulation '{hostId}' has not been registered yet.");
            policy = policyRegistry.Resolve(runtime);
            _policiesByHost.Add(hostId, policy);
        }

        var students = batch.Students.Select(student => student with
        {
            EconomicEffects = policy.Resolve(student.IsEnrolled, student.CompletedStage)
        }).ToArray();
        await batchStore.AddAsync(batch with { Students = students }, cancellationToken);
    }
}
