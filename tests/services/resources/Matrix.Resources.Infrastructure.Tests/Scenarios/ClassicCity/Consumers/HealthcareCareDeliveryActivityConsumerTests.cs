using Matrix.Healthcare.Contracts.Events;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Resources.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class HealthcareCareDeliveryActivityConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_MapsNeutralActivityToClassicCityMedicineDemand()
    {
        HealthcareCareDeliveryActivityV1 message = CreateMessage();
        var mediator = new TestMediator
        {
            Result = new ApplyCityHealthcareMedicineDemandResult(
                ApplyCityHealthcareMedicineDemandStatus.Applied,
                MedicineLoadIndex: 0.05m,
                MedicineStockLevelIndex: 0.57m,
                MedicineShortageRiskIndex: 0.44m,
                SourceRevision: message.SourceRevision)
        };
        var logger = new TestLogger<HealthcareCareDeliveryActivityConsumer>();
        var consumer = new HealthcareCareDeliveryActivityConsumer(mediator, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        ApplyCityHealthcareMedicineDemandCommand command = Assert.Single(mediator.Commands);
        Assert.Equal(message.SimulationHostId, command.CityId);
        Assert.Equal(message.ProcessedPatientCount, command.ProcessedPatientCount);
        Assert.Equal(message.RoutineCareDeliveryCount, command.RoutineCareDeliveryCount);
        Assert.Equal(message.UrgentCareDeliveryCount, command.UrgentCareDeliveryCount);
        Assert.Equal(message.AcuteCareDeliveryCount, command.AcuteCareDeliveryCount);
        Assert.Equal(message.EmergencyCareDeliveryCount, command.EmergencyCareDeliveryCount);
        Assert.Equal(message.SourceRevision, command.SourceRevision);
        Assert.Equal(message.CareDate, command.CareDate);
        Assert.Equal(message.OccurredAtUtc, command.ObservedAtUtc);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
    }

    [Fact]
    public async Task ConsumeAsync_StaleActivity_LogsWarning()
    {
        HealthcareCareDeliveryActivityV1 message = CreateMessage();
        var mediator = new TestMediator
        {
            Result = new ApplyCityHealthcareMedicineDemandResult(
                ApplyCityHealthcareMedicineDemandStatus.Stale,
                MedicineLoadIndex: 0.04m,
                MedicineStockLevelIndex: 0.58m,
                MedicineShortageRiskIndex: 0.43m,
                SourceRevision: 18)
        };
        var logger = new TestLogger<HealthcareCareDeliveryActivityConsumer>();
        var consumer = new HealthcareCareDeliveryActivityConsumer(mediator, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).LogLevel);
    }

    private static HealthcareCareDeliveryActivityV1 CreateMessage()
    {
        return new HealthcareCareDeliveryActivityV1(
            SimulationHostId: ResourcesInfrastructureTestSupport.CityId,
            SourceRevision: 17,
            CareDate: new DateOnly(2048, 5, 6),
            ProcessedPatientCount: 100,
            RoutineCareDeliveryCount: 4,
            UrgentCareDeliveryCount: 3,
            AcuteCareDeliveryCount: 2,
            EmergencyCareDeliveryCount: 1,
            OccurredAtUtc: ResourcesInfrastructureTestSupport.LaterUtc,
            CorrelationId: "health-risk:17:care-delivery");
    }

    private sealed class TestMediator : IMediator
    {
        public List<ApplyCityHealthcareMedicineDemandCommand> Commands { get; } = [];
        public required ApplyCityHealthcareMedicineDemandResult Result { get; init; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(Assert.IsType<ApplyCityHealthcareMedicineDemandCommand>(request));
            return Task.FromResult((TResponse)(object)Result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification => throw new NotSupportedException();
    }
}
