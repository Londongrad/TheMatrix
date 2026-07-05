using System.Data;
using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;
using Matrix.Healthcare.Application.Tests.TestSupport;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Operations.SynchronizeCareMedicineSupply;

public sealed class SynchronizeCareMedicineSupplyCommandHandlerTests
{
    private static readonly Guid HostId = Guid.NewGuid();
    private static readonly DateTimeOffset ObservedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task Handle_FirstSnapshot_CreatesSupplyStateAtomically()
    {
        var repository = new RepositoryStub();
        var unitOfWork = new HealthcareUnitOfWorkStub();
        var handler = new SynchronizeCareMedicineSupplyCommandHandler(
            repository,
            new HealthcareSimulationDeletionRepositoryStub(),
            unitOfWork);

        SynchronizeCareMedicineSupplyResult result = await handler.Handle(
            CreateCommand(sourceRevision: 17),
            CancellationToken.None);

        Assert.Equal(SynchronizeCareMedicineSupplyStatus.Applied, result.Status);
        Assert.True(result.StateCreated);
        Assert.False(result.StateUpdated);
        Assert.NotNull(repository.AddedState);
        Assert.Equal(0.63m, repository.AddedState.StockLevel.Value);
        Assert.Equal(0.31m, repository.AddedState.ShortageRisk.Value);
        Assert.Equal(17, repository.AddedState.LastSourceRevision);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
    }

    [Fact]
    public async Task Handle_NewerSnapshot_UpdatesExistingState()
    {
        CareMedicineSupplyState state = CareMedicineSupplyState.Register(
            new SimulationHostId(HostId),
            new CareAvailabilityIndex(0.70m),
            new CareAvailabilityIndex(0.20m),
            sourceRevision: 16,
            ObservedAtUtc.AddHours(-1));
        var unitOfWork = new HealthcareUnitOfWorkStub();
        var handler = new SynchronizeCareMedicineSupplyCommandHandler(
            new RepositoryStub(state),
            new HealthcareSimulationDeletionRepositoryStub(),
            unitOfWork);

        SynchronizeCareMedicineSupplyResult result = await handler.Handle(
            CreateCommand(sourceRevision: 17),
            CancellationToken.None);

        Assert.False(result.StateCreated);
        Assert.True(result.StateUpdated);
        Assert.Equal(0.63m, state.StockLevel.Value);
        Assert.Equal(17, state.LastSourceRevision);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_StaleSnapshot_DoesNotWrite()
    {
        CareMedicineSupplyState state = CareMedicineSupplyState.Register(
            new SimulationHostId(HostId),
            new CareAvailabilityIndex(0.80m),
            new CareAvailabilityIndex(0.10m),
            sourceRevision: 18,
            ObservedAtUtc.AddHours(1));
        var unitOfWork = new HealthcareUnitOfWorkStub();
        var handler = new SynchronizeCareMedicineSupplyCommandHandler(
            new RepositoryStub(state),
            new HealthcareSimulationDeletionRepositoryStub(),
            unitOfWork);

        SynchronizeCareMedicineSupplyResult result = await handler.Handle(
            CreateCommand(sourceRevision: 17),
            CancellationToken.None);

        Assert.False(result.StateCreated);
        Assert.False(result.StateUpdated);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_DeletedSimulation_DoesNotLoadSupplyState()
    {
        var repository = new RepositoryStub();
        var handler = new SynchronizeCareMedicineSupplyCommandHandler(
            repository,
            new HealthcareSimulationDeletionRepositoryStub(ObservedAtUtc),
            new HealthcareUnitOfWorkStub());

        SynchronizeCareMedicineSupplyResult result = await handler.Handle(
            CreateCommand(sourceRevision: 17),
            CancellationToken.None);

        Assert.Equal(SynchronizeCareMedicineSupplyStatus.SimulationDeleted, result.Status);
        Assert.Equal(0, repository.GetCallCount);
    }

    private static SynchronizeCareMedicineSupplyCommand CreateCommand(long sourceRevision)
    {
        return new SynchronizeCareMedicineSupplyCommand(
            HostId,
            sourceRevision,
            StockLevelIndex: 0.63m,
            ShortageRiskIndex: 0.31m,
            ObservedAtUtc);
    }

    private sealed class RepositoryStub(CareMedicineSupplyState? state = null)
        : ICareMedicineSupplyStateRepository
    {
        internal int GetCallCount { get; private set; }
        internal CareMedicineSupplyState? AddedState { get; private set; }

        public Task<CareMedicineSupplyState?> GetAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            return Task.FromResult(state);
        }

        public Task AddAsync(
            CareMedicineSupplyState stateToAdd,
            CancellationToken cancellationToken = default)
        {
            AddedState = stateToAdd;
            return Task.CompletedTask;
        }
    }
}
