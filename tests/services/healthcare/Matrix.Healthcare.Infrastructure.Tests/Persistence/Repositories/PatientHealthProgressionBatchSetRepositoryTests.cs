using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories;

public sealed class PatientHealthProgressionBatchSetRepositoryTests
{
    private static readonly SimulationHostId SimulationHostId = new(Guid.NewGuid());
    private static readonly DateTimeOffset ReceivedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task AddAndGet_PersistsBatchReceiptMap()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var repository = new PatientHealthProgressionBatchSetRepository(dbContext);
        PatientHealthProgressionBatchSet batchSet = CreateBatchSet(
            totalBatches: 3,
            batchNumber: 2);
        batchSet.RecordCareDeliveryBatch(
            processedPatientCount: 100,
            routineCareDeliveryCount: 4,
            urgentCareDeliveryCount: 3,
            acuteCareDeliveryCount: 2,
            emergencyCareDeliveryCount: 1);

        await repository.AddAsync(batchSet);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        PatientHealthProgressionBatchSet? loaded = await repository.GetAsync(
            SimulationHostId,
            sourceRevision: 17);

        Assert.NotNull(loaded);
        Assert.Equal("health-risk:17", loaded.CorrelationId);
        Assert.Equal(3, loaded.TotalBatches);
        Assert.Equal(1, loaded.ReceivedBatchCount);
        Assert.Equal(new DateOnly(2048, 5, 6), loaded.CurrentDate);
        Assert.Equal(1, loaded.RecordedCareDeliveryBatchCount);
        Assert.Equal(100, loaded.ProcessedPatientCount);
        Assert.Equal(4, loaded.RoutineCareDeliveryCount);
        Assert.Equal(3, loaded.UrgentCareDeliveryCount);
        Assert.Equal(2, loaded.AcuteCareDeliveryCount);
        Assert.Equal(1, loaded.EmergencyCareDeliveryCount);
        Assert.True(loaded.HasReceivedBatch(2));
        Assert.False(loaded.HasReceivedBatch(1));
        Assert.False(loaded.IsComplete);
        Assert.Equal(ReceivedAtUtc, loaded.FirstReceivedAtUtc);
    }

    [Fact]
    public async Task RegisterAfterReload_PersistsCompletedOutOfOrderSet()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using (HealthcareDbContext dbContext =
                     HealthcareInfrastructureTestSupport.CreateDbContext(databaseName))
        {
            var repository = new PatientHealthProgressionBatchSetRepository(dbContext);
            await repository.AddAsync(CreateBatchSet(totalBatches: 3, batchNumber: 3));
            await dbContext.SaveChangesAsync();
        }

        await using (HealthcareDbContext dbContext =
                     HealthcareInfrastructureTestSupport.CreateDbContext(databaseName))
        {
            var repository = new PatientHealthProgressionBatchSetRepository(dbContext);
            PatientHealthProgressionBatchSet loaded = Assert.IsType<PatientHealthProgressionBatchSet>(
                await repository.GetAsync(SimulationHostId, sourceRevision: 17));
            loaded.RegisterBatch(
                "health-risk:17",
                totalBatches: 3,
                batchNumber: 1,
                currentDate: new DateOnly(2048, 5, 6),
                receivedAtUtc: ReceivedAtUtc.AddSeconds(1));
            PatientHealthProgressionBatchRegistrationStatus completion = loaded.RegisterBatch(
                "health-risk:17",
                totalBatches: 3,
                batchNumber: 2,
                currentDate: new DateOnly(2048, 5, 6),
                receivedAtUtc: ReceivedAtUtc.AddSeconds(2));
            await dbContext.SaveChangesAsync();

            Assert.Equal(PatientHealthProgressionBatchRegistrationStatus.Completed, completion);
        }

        await using (HealthcareDbContext dbContext =
                     HealthcareInfrastructureTestSupport.CreateDbContext(databaseName))
        {
            var repository = new PatientHealthProgressionBatchSetRepository(dbContext);
            PatientHealthProgressionBatchSet completed =
                Assert.IsType<PatientHealthProgressionBatchSet>(
                    await repository.GetAsync(SimulationHostId, sourceRevision: 17));

            Assert.True(completed.IsComplete);
            Assert.Equal(3, completed.ReceivedBatchCount);
            Assert.Equal(ReceivedAtUtc.AddSeconds(2), completed.CompletedAtUtc);
            Assert.True(completed.HasReceivedBatch(1));
            Assert.True(completed.HasReceivedBatch(2));
            Assert.True(completed.HasReceivedBatch(3));
        }
    }

    [Fact]
    public async Task Get_DifferentHostOrRevision_DoesNotReturnBatchSet()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var repository = new PatientHealthProgressionBatchSetRepository(dbContext);
        await repository.AddAsync(CreateBatchSet(totalBatches: 1, batchNumber: 1));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        PatientHealthProgressionBatchSet? differentHost = await repository.GetAsync(
            new SimulationHostId(Guid.NewGuid()),
            sourceRevision: 17);
        PatientHealthProgressionBatchSet? differentRevision = await repository.GetAsync(
            SimulationHostId,
            sourceRevision: 18);

        Assert.Null(differentHost);
        Assert.Null(differentRevision);
    }

    private static PatientHealthProgressionBatchSet CreateBatchSet(
        int totalBatches,
        int batchNumber)
    {
        return PatientHealthProgressionBatchSet.Start(
            simulationHostId: SimulationHostId,
            sourceRevision: 17,
            correlationId: "health-risk:17",
            totalBatches: totalBatches,
            batchNumber: batchNumber,
            currentDate: new DateOnly(2048, 5, 6),
            receivedAtUtc: ReceivedAtUtc);
    }
}
