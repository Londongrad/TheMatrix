using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence
{
    public sealed class HealthcareOutboxPersistenceTests
    {
        [Fact]
        public async Task SaveAndReload_PreservesOutboxEnvelope()
        {
            await using HealthcareDbContext dbContext =
                HealthcareInfrastructureTestSupport.CreateDbContext();
            OutboxMessage message = OutboxMessage.Create(
                type: "healthcare.test.v1",
                occurredOnUtc: new DateTime(2048, 5, 6, 10, 0, 0, DateTimeKind.Utc),
                payload: new { Value = 42 });

            dbContext.OutboxMessages.Add(message);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            OutboxMessage loaded = await dbContext.OutboxMessages.SingleAsync();

            Assert.Equal(message.Id, loaded.Id);
            Assert.Equal("healthcare.test.v1", loaded.Type);
            Assert.Contains("42", loaded.PayloadJson, StringComparison.Ordinal);
        }
    }
}
