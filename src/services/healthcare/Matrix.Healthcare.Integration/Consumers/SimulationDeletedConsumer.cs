using MassTransit;
using Matrix.Healthcare.Application.Lifecycle.DeleteHealthcareSimulation;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Consumers
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
            DeleteHealthcareSimulationResult result = await mediator.Send(
                request: new DeleteHealthcareSimulationCommand(
                    SimulationHostId: message.HostId,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case DeleteHealthcareSimulationStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted healthcare data for simulationHostId={SimulationHostId}.",
                        message.HostId);
                    break;
                case DeleteHealthcareSimulationStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate healthcare deletion for simulationHostId={SimulationHostId}.",
                        message.HostId);
                    break;
                case DeleteHealthcareSimulationStatus.Stale:
                    logger.LogWarning(
                        message: "Ignored stale healthcare deletion for simulationHostId={SimulationHostId}.",
                        message.HostId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
            }
        }
    }
}
