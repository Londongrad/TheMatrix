using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class SimulationCoreOutboxWriterEducationProvisioningTests
{
    [Fact]
    public async Task AddEducationInstitutionProvisioningAsync_WritesProvisioningBatch()
    {
        using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddEducationInstitutionProvisioningAsync_WritesProvisioningBatch));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(20);
        DateTimeOffset synchronizedAtUtc = OutboxTestSupport.BaseUtc.AddMinutes(15);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        var simulationHostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var institutionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var batch = new EducationInstitutionProvisioningBatch(
            SimulationHostId: simulationHostId,
            SourceRevision: 3,
            SynchronizedAtUtc: synchronizedAtUtc,
            CorrelationId: "simulation:aaaaaaaa:education-institutions:3",
            Institutions:
            [
                new EducationInstitutionProvisioning(
                    InstitutionId: institutionId,
                    Name: "Central Education Complex",
                    Kind: "School",
                    LocationAnchorId: institutionId,
                    Capacity: 640,
                    IsActive: true)
            ]);

        await writer.AddEducationInstitutionProvisioningAsync(batch, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        OutboxMessage message = await dbContext.OutboxMessages.AsNoTracking().SingleAsync();
        SimulationEducationInstitutionProvisioningBatchV1 payload =
            OutboxTestSupport.DeserializePayload<SimulationEducationInstitutionProvisioningBatchV1>(message);

        Assert.Equal(
            expected: SimulationCoreEventTypes.SimulationEducationInstitutionProvisioningBatchV1,
            actual: message.Type);
        Assert.Equal(
            expected: occurredOnUtc.UtcDateTime,
            actual: message.OccurredOnUtc);
        Assert.Equal(
            expected: simulationHostId,
            actual: payload.SimulationHostId);
        Assert.Equal(
            expected: 3,
            actual: payload.SourceRevision);
        Assert.Equal(
            expected: synchronizedAtUtc,
            actual: payload.SynchronizedAtUtc);
        Assert.Equal(
            expected: batch.CorrelationId,
            actual: payload.CorrelationId);
        Assert.Equal(
            expected: 1,
            actual: payload.BatchNumber);
        Assert.Equal(
            expected: 1,
            actual: payload.TotalBatches);
        SimulationEducationInstitutionProvisioningV1 institution = Assert.Single(payload.Institutions);
        Assert.Equal(
            expected: institutionId,
            actual: institution.InstitutionId);
        Assert.Equal(
            expected: "Central Education Complex",
            actual: institution.Name);
        Assert.Equal(
            expected: "School",
            actual: institution.Kind);
        Assert.Equal(
            expected: institutionId,
            actual: institution.LocationAnchorId);
        Assert.Equal(
            expected: 640,
            actual: institution.Capacity);
        Assert.True(institution.IsActive);
    }

    [Fact]
    public async Task AddEducationInstitutionProvisioningAsync_WhenEmpty_DoesNotWriteMessage()
    {
        using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddEducationInstitutionProvisioningAsync_WhenEmpty_DoesNotWriteMessage));
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));
        var batch = new EducationInstitutionProvisioningBatch(
            SimulationHostId: Guid.NewGuid(),
            SourceRevision: 0,
            SynchronizedAtUtc: OutboxTestSupport.BaseUtc,
            CorrelationId: "empty",
            Institutions: []);

        await writer.AddEducationInstitutionProvisioningAsync(batch, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.Empty(await dbContext.OutboxMessages.AsNoTracking().ToArrayAsync());
    }
}
