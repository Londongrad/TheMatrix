using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class SimulationCoreOutboxWriterFacilityProvisioningTests
{
    [Fact]
    public async Task AddCareFacilityProvisioningAsync_WritesProvisioningBatch()
    {
        using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddCareFacilityProvisioningAsync_WritesProvisioningBatch));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(20);
        DateTimeOffset synchronizedAtUtc = OutboxTestSupport.BaseUtc.AddMinutes(15);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        Guid simulationHostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid facilityId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var batch = new CareFacilityProvisioningBatch(
            SimulationHostId: simulationHostId,
            SourceRevision: 3,
            SynchronizedAtUtc: synchronizedAtUtc,
            CorrelationId: "simulation:aaaaaaaa:care-facilities:3",
            Facilities:
            [
                new CareFacilityProvisioning(
                    FacilityId: facilityId,
                    Name: "Central Hospital",
                    Kind: "Hospital",
                    LocationAnchorId: facilityId,
                    DailyPatientCapacity: 450,
                    IsActive: true)
            ]);

        await writer.AddCareFacilityProvisioningAsync(batch, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        OutboxMessage message = await dbContext.OutboxMessages.AsNoTracking().SingleAsync();
        SimulationCareFacilityProvisioningBatchV1 payload =
            OutboxTestSupport.DeserializePayload<SimulationCareFacilityProvisioningBatchV1>(message);

        Assert.Equal(SimulationCoreEventTypes.SimulationCareFacilityProvisioningBatchV1, message.Type);
        Assert.Equal(occurredOnUtc.UtcDateTime, message.OccurredOnUtc);
        Assert.Equal(simulationHostId, payload.SimulationHostId);
        Assert.Equal(3, payload.SourceRevision);
        Assert.Equal(synchronizedAtUtc, payload.SynchronizedAtUtc);
        Assert.Equal(batch.CorrelationId, payload.CorrelationId);
        Assert.Equal(1, payload.BatchNumber);
        Assert.Equal(1, payload.TotalBatches);
        SimulationCareFacilityProvisioningV1 facility = Assert.Single(payload.Facilities);
        Assert.Equal(facilityId, facility.FacilityId);
        Assert.Equal("Central Hospital", facility.Name);
        Assert.Equal("Hospital", facility.Kind);
        Assert.Equal(facilityId, facility.LocationAnchorId);
        Assert.Equal(450, facility.DailyPatientCapacity);
        Assert.True(facility.IsActive);
    }

    [Fact]
    public async Task AddCareFacilityProvisioningAsync_WhenEmpty_DoesNotWriteMessage()
    {
        using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddCareFacilityProvisioningAsync_WhenEmpty_DoesNotWriteMessage));
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));
        var batch = new CareFacilityProvisioningBatch(
            SimulationHostId: Guid.NewGuid(),
            SourceRevision: 0,
            SynchronizedAtUtc: OutboxTestSupport.BaseUtc,
            CorrelationId: "empty",
            Facilities: []);

        await writer.AddCareFacilityProvisioningAsync(batch, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.Empty(await dbContext.OutboxMessages.AsNoTracking().ToArrayAsync());
    }
}
