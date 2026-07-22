using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Integration;
using Matrix.Education.Application.Scenarios.ClassicCity.Participation;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Contracts.Events;
using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Application.Tests.Integration;

public sealed class EducationStudentParticipationPublisherTests
{
    [Fact]
    public async Task AddAsync_UsesHostPolicyAndReadsRuntimeOnceAcrossChunks()
    {
        var repository = new EducationSimulationRuntimeRepositoryStub();
        var cityPolicy = new ClassicCityEducationEconomicPolicy();
        var otherPolicy = new OtherPolicy();
        var firstHost = new SimulationHostId(Guid.NewGuid());
        var secondHost = new SimulationHostId(Guid.NewGuid());
        repository.Runtimes[firstHost] = cityPolicy.RuntimeKey;
        repository.Runtimes[secondHost] = otherPolicy.RuntimeKey;
        var store = new BatchStoreStub();
        var publisher = new EducationStudentParticipationPublisher(repository, new([cityPolicy, otherPolicy]), store);

        await publisher.AddAsync(Batch(firstHost.Value, 1));
        await publisher.AddAsync(Batch(secondHost.Value, 1));
        await publisher.AddAsync(Batch(firstHost.Value, 2));

        Assert.Equal(2, repository.ReadCount);
        Assert.Equal(3, store.Batches.Count);
        Assert.Equal(4m, store.Batches[0].Students[0].EconomicEffects!.TransferIncome[0].DailyIncome);
        Assert.Equal(99m, store.Batches[1].Students[0].EconomicEffects!.TransferIncome[0].DailyIncome);
        Assert.Same(store.Batches[0].Students[0].EconomicEffects, store.Batches[2].Students[0].EconomicEffects);
        Assert.Equal(2, store.Batches[2].BatchNumber);
        Assert.Equal(3, store.Batches[0].Students[0].ParticipationRevision);
    }

    [Fact]
    public async Task AddAsync_RejectsMissingAndUnsupportedRuntimeWithoutWritingOutbox()
    {
        var repository = new EducationSimulationRuntimeRepositoryStub();
        var store = new BatchStoreStub();
        var publisher = new EducationStudentParticipationPublisher(repository, new([new ClassicCityEducationEconomicPolicy()]), store);
        var hostId = new SimulationHostId(Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.AddAsync(Batch(hostId.Value, 1)));
        repository.Runtimes[hostId] = new(new SimulationScenarioKey("unsupported"), new SimulationHostTypeKey("city"));
        await Assert.ThrowsAsync<NotSupportedException>(() => publisher.AddAsync(Batch(hostId.Value, 1)));
        Assert.Empty(store.Batches);
    }

    [Fact]
    public async Task AddAsync_WithdrawalPublishesExplicitNeutralSupport()
    {
        var policy = new ClassicCityEducationEconomicPolicy();
        var repository = new EducationSimulationRuntimeRepositoryStub();
        var host = new SimulationHostId(Guid.NewGuid());
        repository.Runtimes[host] = policy.RuntimeKey;
        var store = new BatchStoreStub();
        var publisher = new EducationStudentParticipationPublisher(repository, new([policy]), store);
        var batch = Batch(host.Value, 1);
        await publisher.AddAsync(batch with { Students = [batch.Students[0] with { IsEnrolled = false }] });
        var effects = Assert.Single(store.Batches).Students[0].EconomicEffects!;
        Assert.Equal(0m, Assert.Single(effects.TransferIncome).DailyIncome);
        Assert.Equal(6m, effects.EmploymentIncomeBonus);
        Assert.Equal(1d, effects.EmploymentAvailabilityFactor);
    }

    private static EducationStudentParticipationBatchV1 Batch(Guid hostId, int batchNumber) => new(
        hostId, new DateOnly(2048, 9, 1), DateTimeOffset.UtcNow, "test", batchNumber, 2,
        [new(Guid.NewGuid(), 3, 2, true, "higher", Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2048, 9, 1),
            "upper-secondary", new DateOnly(2048, 6, 30))]);

    private sealed class BatchStoreStub : IEducationStudentParticipationBatchStore
    {
        public List<EducationStudentParticipationBatchV1> Batches { get; } = [];
        public Task AddAsync(EducationStudentParticipationBatchV1 batch, CancellationToken cancellationToken = default)
        {
            Batches.Add(batch);
            return Task.CompletedTask;
        }
    }

    private sealed class OtherPolicy : IEducationParticipationEconomicPolicy
    {
        public SimulationRuntimeKey RuntimeKey { get; } = new(new SimulationScenarioKey("test-scenario"), new SimulationHostTypeKey("network"));
        public EducationEconomicEffectsV1 Resolve(bool isEnrolled, string? completedStage) => new([new(0, 99m)], 0m, 0d, 1d, 0m, 0m, 0m);
    }
}
