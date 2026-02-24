using MassTransit;
using Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions;

namespace Matrix.ApiGateway.Consumers
{
    public sealed class ClassicCitySetupLaunchRequestedConsumer(
        IClassicCitySetupSessionService setupSessionService)
        : IConsumer<ClassicCitySetupLaunchRequested>
    {
        public Task Consume(ConsumeContext<ClassicCitySetupLaunchRequested> context)
        {
            return setupSessionService.ProcessLaunchAsync(
                sessionId: context.Message.SessionId,
                cancellationToken: context.CancellationToken);
        }
    }
}
