using MassTransit;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;

namespace Matrix.ApiGateway.Consumers
{
    public sealed class ClassicCitySetupLaunchRequestedConsumer(IClassicCitySetupSessionService setupSessionService)
        : IConsumer<ClassicCitySetupLaunchRequested>
    {
        public Task Consume(ConsumeContext<ClassicCitySetupLaunchRequested> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal Task ConsumeAsync(
            ClassicCitySetupLaunchRequested message,
            CancellationToken cancellationToken)
        {
            return setupSessionService.ProcessLaunchAsync(
                sessionId: message.SessionId,
                cancellationToken: cancellationToken);
        }
    }
}
