using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class SimulationCoreOutboxWriterCityEventTests
{
    [Fact]
    public async Task AddCityEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddCityEventsAsync_WhenDomainEventsAreEmpty_DoesNotAddMessages));
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(OutboxTestSupport.BaseUtc));

        await writer.AddCityEventsAsync([], CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.Empty(await dbContext.OutboxMessages.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AddCityEventsAsync_WhenCityCreatedEventIsAdded_WritesCreatedAndEnvironmentChangedMessages()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddCityEventsAsync_WhenCityCreatedEventIsAdded_WritesCreatedAndEnvironmentChangedMessages));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(15);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        CityCreatedDomainEvent domainEvent = OutboxTestSupport.CreateCityCreatedDomainEvent();

        await writer.AddCityEventsAsync([domainEvent], CancellationToken.None);
        await dbContext.SaveChangesAsync();

        List<OutboxMessage> messages = await dbContext.OutboxMessages
           .AsNoTracking()
           .OrderBy(x => x.Type)
           .ToListAsync();

        Assert.Equal(2, messages.Count);

        OutboxMessage cityCreatedMessage = Assert.Single(messages, x => x.Type == IntegrationEventTypes.CityCreatedV1);
        CityCreatedV1 cityCreatedPayload = OutboxTestSupport.DeserializePayload<CityCreatedV1>(cityCreatedMessage);
        Assert.Equal(occurredOnUtc.UtcDateTime, cityCreatedMessage.OccurredOnUtc);
        Assert.Equal(domainEvent.CityId.Value, cityCreatedPayload.CityId);
        Assert.Equal(domainEvent.Name.Value, cityCreatedPayload.Name);
        Assert.Equal(domainEvent.SimulationKind.ToString(), cityCreatedPayload.SimulationKind);
        Assert.Equal(domainEvent.CreatedAtUtc, cityCreatedPayload.CreatedAtUtc);
        Assert.Equal(domainEvent.GenerationProfile.DevelopmentLevel.ToString(), cityCreatedPayload.DevelopmentLevel);
        Assert.Equal(domainEvent.GenerationProfile.EconomyProfile.ToString(), cityCreatedPayload.EconomyProfile);
        Assert.Equal(domainEvent.RunId, cityCreatedPayload.RunId);
        Assert.Equal(domainEvent.GenerationSeed.Value, cityCreatedPayload.SimulationSeed);
        Assert.Equal(domainEvent.ScenarioModelSetVersion.Value, cityCreatedPayload.ScenarioModelSetVersion);

        OutboxMessage environmentChangedMessage = Assert.Single(messages, x => x.Type == IntegrationEventTypes.CityEnvironmentChangedV1);
        CityEnvironmentChangedV1 environmentChangedPayload =
            OutboxTestSupport.DeserializePayload<CityEnvironmentChangedV1>(environmentChangedMessage);
        Assert.Equal(occurredOnUtc.UtcDateTime, environmentChangedMessage.OccurredOnUtc);
        Assert.Equal(domainEvent.CityId.Value, environmentChangedPayload.CityId);
        Assert.Null(environmentChangedPayload.PreviousEnvironment);
        Assert.NotNull(environmentChangedPayload.CurrentEnvironment);
        Assert.Equal(domainEvent.Environment.ClimateZone.ToString(), environmentChangedPayload.CurrentEnvironment!.ClimateZone);
        Assert.Equal(domainEvent.Environment.Hemisphere.ToString(), environmentChangedPayload.CurrentEnvironment.Hemisphere);
        Assert.Equal((int)domainEvent.Environment.UtcOffset.TotalMinutes, environmentChangedPayload.CurrentEnvironment.UtcOffsetMinutes);
        Assert.Equal(occurredOnUtc, environmentChangedPayload.OccurredOnUtc);
    }

    [Fact]
    public async Task AddCityEventsAsync_WhenArchiveDeleteAndEnvironmentChangeEventsAreAdded_WritesMatchingMessages()
    {
        using var dbContext = OutboxTestSupport.CreateDbContext(
            nameof(AddCityEventsAsync_WhenArchiveDeleteAndEnvironmentChangeEventsAreAdded_WritesMatchingMessages));
        DateTimeOffset occurredOnUtc = OutboxTestSupport.BaseUtc.AddMinutes(25);
        var writer = new SimulationCoreOutboxWriter(
            dbContext,
            OutboxTestSupport.CreateTimeProvider(occurredOnUtc));
        Guid cityId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var archivedEvent = new CityArchivedDomainEvent(
            new Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.CityId(cityId),
            OutboxTestSupport.BaseUtc.AddHours(5));
        var deletedEvent = new CityDeletedDomainEvent(
            new Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.CityId(cityId),
            OutboxTestSupport.BaseUtc.AddHours(6));
        CityEnvironmentChangedDomainEvent environmentChangedEvent =
            OutboxTestSupport.CreateCityEnvironmentChangedDomainEvent(cityId);

        await writer.AddCityEventsAsync([archivedEvent, deletedEvent, environmentChangedEvent], CancellationToken.None);
        await dbContext.SaveChangesAsync();

        List<OutboxMessage> messages = await dbContext.OutboxMessages
           .AsNoTracking()
           .OrderBy(x => x.Type)
           .ToListAsync();

        Assert.Equal(3, messages.Count);

        CityArchivedV1 archivedPayload = OutboxTestSupport.DeserializePayload<CityArchivedV1>(
            Assert.Single(messages, x => x.Type == IntegrationEventTypes.CityArchivedV1));
        Assert.Equal(cityId, archivedPayload.CityId);
        Assert.Equal(archivedEvent.ArchivedAtUtc, archivedPayload.ArchivedAtUtc);

        CityDeletedV1 deletedPayload = OutboxTestSupport.DeserializePayload<CityDeletedV1>(
            Assert.Single(messages, x => x.Type == IntegrationEventTypes.CityDeletedV1));
        Assert.Equal(cityId, deletedPayload.CityId);
        Assert.Equal(deletedEvent.DeletedAtUtc, deletedPayload.DeletedAtUtc);

        CityEnvironmentChangedV1 environmentChangedPayload = OutboxTestSupport.DeserializePayload<CityEnvironmentChangedV1>(
            Assert.Single(messages, x => x.Type == IntegrationEventTypes.CityEnvironmentChangedV1));
        Assert.Equal(cityId, environmentChangedPayload.CityId);
        Assert.NotNull(environmentChangedPayload.PreviousEnvironment);
        Assert.Equal(environmentChangedEvent.From.ClimateZone.ToString(), environmentChangedPayload.PreviousEnvironment!.ClimateZone);
        Assert.Equal(environmentChangedEvent.To.ClimateZone.ToString(), environmentChangedPayload.CurrentEnvironment.ClimateZone);
        Assert.Equal(environmentChangedEvent.To.Hemisphere.ToString(), environmentChangedPayload.CurrentEnvironment.Hemisphere);
        Assert.Equal((int)environmentChangedEvent.To.UtcOffset.TotalMinutes, environmentChangedPayload.CurrentEnvironment.UtcOffsetMinutes);
        Assert.Equal(occurredOnUtc, environmentChangedPayload.OccurredOnUtc);
    }
}
