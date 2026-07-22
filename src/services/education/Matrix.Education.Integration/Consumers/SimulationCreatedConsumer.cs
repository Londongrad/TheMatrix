using MassTransit;
using Matrix.Education.Application.Lifecycle.RegisterEducationSimulation;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;

namespace Matrix.Education.Integration.Consumers;

public sealed class SimulationCreatedConsumer(IMediator mediator) : IConsumer<SimulationCreatedV1>
{
    public Task Consume(ConsumeContext<SimulationCreatedV1> context) =>
        mediator.Send(Map(context.Message), context.CancellationToken);

    internal static RegisterEducationSimulationCommand Map(SimulationCreatedV1 message) =>
        new(message.HostId, message.ScenarioKey, message.HostTypeKey);
}
