using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Operations.SynchronizeCareServiceQuality;

public sealed class SynchronizeCareServiceQualityCommandHandlerTests
{
    private static readonly Guid HostId = Guid.NewGuid();
    private static readonly DateTimeOffset ObservedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task Handle_FirstObservation_CreatesStateAtomically()
    {
        var repository = new RepositoryStub();
        var unitOfWork = new HealthcareUnitOfWorkStub();
        var handler = new SynchronizeCareServiceQualityCommandHandler(
            repository,
            new HealthcareSimulationDeletionRepositoryStub(),
            unitOfWork);

        SynchronizeCareServiceQualityResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(SynchronizeCareServiceQualityStatus.Applied, result.Status);
        Assert.True(result.StateCreated);
        Assert.False(result.StateUpdated);
        Assert.NotNull(repository.AddedState);
        Assert.Equal(0.82m, repository.AddedState.QualityMultiplier.Value);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
    }

    [Fact]
    public async Task Handle_NewerObservation_UpdatesExistingState()
    {
        CareServiceQualityState state = CareServiceQualityState.Register(
            new SimulationHostId(HostId),
            new CareQualityMultiplier(0.70m),
            ObservedAtUtc.AddHours(-1));
        var repository = new RepositoryStub(state);
        var unitOfWork = new HealthcareUnitOfWorkStub();
        var handler = new SynchronizeCareServiceQualityCommandHandler(
            repository,
            new HealthcareSimulationDeletionRepositoryStub(),
            unitOfWork);

        SynchronizeCareServiceQualityResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.False(result.StateCreated);
        Assert.True(result.StateUpdated);
        Assert.Equal(0.82m, state.QualityMultiplier.Value);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_StaleObservation_DoesNotWrite()
    {
        CareServiceQualityState state = CareServiceQualityState.Register(
            new SimulationHostId(HostId),
            new CareQualityMultiplier(0.90m),
            ObservedAtUtc.AddHours(1));
        var unitOfWork = new HealthcareUnitOfWorkStub();
        var handler = new SynchronizeCareServiceQualityCommandHandler(
            new RepositoryStub(state),
            new HealthcareSimulationDeletionRepositoryStub(),
            unitOfWork);

        SynchronizeCareServiceQualityResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.False(result.StateCreated);
        Assert.False(result.StateUpdated);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_DeletedSimulation_DoesNotLoadState()
    {
        var repository = new RepositoryStub();
        var handler = new SynchronizeCareServiceQualityCommandHandler(
            repository,
            new HealthcareSimulationDeletionRepositoryStub(ObservedAtUtc),
            new HealthcareUnitOfWorkStub());

        SynchronizeCareServiceQualityResult result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(SynchronizeCareServiceQualityStatus.SimulationDeleted, result.Status);
        Assert.Equal(0, repository.GetCallCount);
    }

    private static SynchronizeCareServiceQualityCommand CreateCommand()
    {
        return new SynchronizeCareServiceQualityCommand(
            HostId,
            QualityMultiplier: 0.82m,
            ObservedAtUtc);
    }

    private sealed class RepositoryStub(CareServiceQualityState? state = null)
        : ICareServiceQualityStateRepository
    {
        internal int GetCallCount { get; private set; }
        internal CareServiceQualityState? AddedState { get; private set; }

        public Task<CareServiceQualityState?> GetAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            return Task.FromResult(state);
        }

        public Task AddAsync(
            CareServiceQualityState stateToAdd,
            CancellationToken cancellationToken = default)
        {
            AddedState = stateToAdd;
            return Task.CompletedTask;
        }
    }
}
