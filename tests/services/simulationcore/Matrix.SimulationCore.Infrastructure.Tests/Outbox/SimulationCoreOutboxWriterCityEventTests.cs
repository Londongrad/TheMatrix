using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox
{
    public sealed class SimulationCoreOutboxWriterCityEventTests
    {
        [Fact]
        public async Task AddCityEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddCityEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages));
            var writer = new SimulationCoreOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));

            await writer.AddCityEventsAsync(
                domainEvents: [],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            Assert.Empty(
                await dbContext.OutboxMessages.AsNoTracking()
                   .ToListAsync());
        }

        [Fact]
        public async Task AddCityEventsAsync_WhenCityCreatedEventIsAdded_WritesCreatedAndEnvironmentChangedMessages()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddCityEventsAsync_WhenCityCreatedEventIsAdded_WritesCreatedAndEnvironmentChangedMessages));
            DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(15);
            var writer = new SimulationCoreOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
            CityCreatedDomainEvent domainEvent = OutboxTestSupport.CreateCityCreatedDomainEvent();

            await writer.AddCityEventsAsync(
                domainEvents: [domainEvent],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            List<OutboxMessage> messages = await dbContext.OutboxMessages
               .AsNoTracking()
               .OrderBy(x => x.Type)
               .ToListAsync();

            Assert.Equal(
                expected: 2,
                actual: messages.Count);

            OutboxMessage cityCreatedMessage = Assert.Single(
                collection: messages,
                predicate: x => x.Type == IntegrationEventTypes.CityCreatedV1);
            CityCreatedV1 cityCreatedPayload = OutboxTestSupport.DeserializePayload<CityCreatedV1>(cityCreatedMessage);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: cityCreatedMessage.OccurredOnUtc);
            Assert.Equal(
                expected: domainEvent.CityId.Value,
                actual: cityCreatedPayload.CityId);
            Assert.Equal(
                expected: domainEvent.Name.Value,
                actual: cityCreatedPayload.Name);
            Assert.Equal(
                expected: domainEvent.SimulationKind.ToString(),
                actual: cityCreatedPayload.SimulationKind);
            Assert.Equal(
                expected: domainEvent.CreatedAtUtc,
                actual: cityCreatedPayload.CreatedAtUtc);
            Assert.Equal(
                expected: domainEvent.GenerationProfile.DevelopmentLevel.ToString(),
                actual: cityCreatedPayload.DevelopmentLevel);
            Assert.Equal(
                expected: domainEvent.GenerationProfile.EconomyProfile.ToString(),
                actual: cityCreatedPayload.EconomyProfile);
            Assert.Equal(
                expected: domainEvent.RunId,
                actual: cityCreatedPayload.RunId);
            Assert.Equal(
                expected: domainEvent.GenerationSeed.Value,
                actual: cityCreatedPayload.SimulationSeed);
            Assert.Equal(
                expected: domainEvent.ScenarioModelSetVersion.Value,
                actual: cityCreatedPayload.ScenarioModelSetVersion);

            OutboxMessage environmentChangedMessage = Assert.Single(
                collection: messages,
                predicate: x => x.Type == IntegrationEventTypes.CityEnvironmentChangedV1);
            CityEnvironmentChangedV1 environmentChangedPayload =
                OutboxTestSupport.DeserializePayload<CityEnvironmentChangedV1>(environmentChangedMessage);
            Assert.Equal(
                expected: occurredOnUtc.UtcDateTime,
                actual: environmentChangedMessage.OccurredOnUtc);
            Assert.Equal(
                expected: domainEvent.CityId.Value,
                actual: environmentChangedPayload.CityId);
            Assert.Null(environmentChangedPayload.PreviousEnvironment);
            Assert.NotNull(environmentChangedPayload.CurrentEnvironment);
            Assert.Equal(
                expected: domainEvent.Environment.ClimateZone.ToString(),
                actual: environmentChangedPayload.CurrentEnvironment!.ClimateZone);
            Assert.Equal(
                expected: domainEvent.Environment.Hemisphere.ToString(),
                actual: environmentChangedPayload.CurrentEnvironment.Hemisphere);
            Assert.Equal(
                expected: domainEvent.Environment.UtcOffset.TotalMinutes,
                actual: environmentChangedPayload.CurrentEnvironment.UtcOffsetMinutes);
            Assert.Equal(
                expected: occurredOnUtc,
                actual: environmentChangedPayload.OccurredOnUtc);
        }

        [Fact]
        public async Task AddCityEventsAsync_WhenEnvironmentChanges_WritesMatchingMessage()
        {
            using SimulationCoreDbContext dbContext = OutboxTestSupport.CreateDbContext(
                nameof(AddCityEventsAsync_WhenEnvironmentChanges_WritesMatchingMessage));
            DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(25);
            var writer = new SimulationCoreOutboxWriter(
                dbContext: dbContext,
                timeProvider: OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
            var cityId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            CityEnvironmentChangedDomainEvent environmentChangedEvent =
                OutboxTestSupport.CreateCityEnvironmentChangedDomainEvent(cityId);

            await writer.AddCityEventsAsync(
                domainEvents: [environmentChangedEvent],
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            List<OutboxMessage> messages = await dbContext.OutboxMessages
               .AsNoTracking()
               .OrderBy(x => x.Type)
               .ToListAsync();

            CityEnvironmentChangedV1 environmentChangedPayload =
                OutboxTestSupport.DeserializePayload<CityEnvironmentChangedV1>(
                    Assert.Single(messages));
            Assert.Equal(
                expected: cityId,
                actual: environmentChangedPayload.CityId);
            Assert.NotNull(environmentChangedPayload.PreviousEnvironment);
            Assert.Equal(
                expected: environmentChangedEvent.From.ClimateZone.ToString(),
                actual: environmentChangedPayload.PreviousEnvironment!.ClimateZone);
            Assert.Equal(
                expected: environmentChangedEvent.To.ClimateZone.ToString(),
                actual: environmentChangedPayload.CurrentEnvironment.ClimateZone);
            Assert.Equal(
                expected: environmentChangedEvent.To.Hemisphere.ToString(),
                actual: environmentChangedPayload.CurrentEnvironment.Hemisphere);
            Assert.Equal(
                expected: environmentChangedEvent.To.UtcOffset.TotalMinutes,
                actual: environmentChangedPayload.CurrentEnvironment.UtcOffsetMinutes);
            Assert.Equal(
                expected: occurredOnUtc,
                actual: environmentChangedPayload.OccurredOnUtc);
        }
    }
}
