using MassTransit;
using Matrix.Education.Application.Lifecycle.DeleteEducationSimulation;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Education.Integration.Consumers
{
    public sealed class SimulationDeletedConsumer(
        IMediator mediator,
        ILogger<SimulationDeletedConsumer> logger)
        : IConsumer<SimulationDeletedV1>
    {
        public Task Consume(ConsumeContext<SimulationDeletedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            SimulationDeletedV1 message,
            CancellationToken cancellationToken)
        {
            DeleteEducationSimulationResult result = await mediator.Send(
                request: new DeleteEducationSimulationCommand(
                    SimulationHostId: message.HostId,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case DeleteEducationSimulationStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted education data for simulationHostId={SimulationHostId}.",
                        message.HostId);
                    break;
                case DeleteEducationSimulationStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate education deletion for simulationHostId={SimulationHostId}.",
                        message.HostId);
                    break;
                case DeleteEducationSimulationStatus.Stale:
                    logger.LogWarning(
                        message: "Ignored stale education deletion for simulationHostId={SimulationHostId}.",
                        message.HostId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
            }
        }
    }
}
