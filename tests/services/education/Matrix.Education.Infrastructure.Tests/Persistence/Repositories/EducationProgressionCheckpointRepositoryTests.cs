using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Persistence.Repositories;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class EducationProgressionCheckpointRepositoryTests
    {
        private static readonly DateTimeOffset InitialUtc =
            new(2048, 1, 1, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task AddAndGet_PersistsOneCheckpointPerSimulationHost()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var repository = new EducationProgressionCheckpointRepository(dbContext);
            var unitOfWork = new EducationUnitOfWork(dbContext);
            var hostId = new SimulationHostId(Guid.NewGuid());
            EducationProgressionCheckpoint checkpoint =
                EducationProgressionCheckpoint.CreateCompleted(
                    simulationHostId: hostId,
                    tickId: 12,
                    completedAtUtc: InitialUtc,
                    updatedAtUtc: InitialUtc);

            await repository.AddAsync(checkpoint);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();

            EducationProgressionCheckpoint? loaded = await repository.GetAsync(hostId);

            Assert.NotNull(loaded);
            Assert.Equal(hostId, loaded.SimulationHostId);
            Assert.Equal(12, loaded.LastCompletedTickId);
            Assert.Equal(InitialUtc, loaded.LastCompletedAtUtc);
        }

        [Fact]
        public async Task Get_AfterAdvancingCheckpoint_ReturnsLatestTick()
        {
            await using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            var repository = new EducationProgressionCheckpointRepository(dbContext);
            var unitOfWork = new EducationUnitOfWork(dbContext);
            var hostId = new SimulationHostId(Guid.NewGuid());
            EducationProgressionCheckpoint checkpoint =
                EducationProgressionCheckpoint.CreateCompleted(
                    simulationHostId: hostId,
                    tickId: 12,
                    completedAtUtc: InitialUtc,
                    updatedAtUtc: InitialUtc);
            await repository.AddAsync(checkpoint);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);

            checkpoint.MarkCompleted(
                tickId: 13,
                completedAtUtc: InitialUtc.AddHours(6),
                updatedAtUtc: InitialUtc.AddMinutes(1));
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();

            EducationProgressionCheckpoint? loaded = await repository.GetAsync(hostId);

            Assert.NotNull(loaded);
            Assert.Equal(13, loaded.LastCompletedTickId);
            Assert.Equal(InitialUtc.AddHours(6), loaded.LastCompletedAtUtc);
        }
    }
}
