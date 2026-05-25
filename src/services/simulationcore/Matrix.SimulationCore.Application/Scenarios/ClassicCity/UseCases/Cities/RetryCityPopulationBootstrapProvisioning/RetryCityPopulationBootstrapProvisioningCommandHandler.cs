using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.
    RetryCityPopulationBootstrapProvisioning
{
    public sealed class RetryCityPopulationBootstrapProvisioningCommandHandler(
        IMediator mediator,
        IClassicCityProvisioningOrchestrator orchestrator)
        : IRequestHandler<RetryCityPopulationBootstrapProvisioningCommand,
            RetryCityPopulationBootstrapProvisioningResult>
    {
        public async Task<RetryCityPopulationBootstrapProvisioningResult> Handle(
            RetryCityPopulationBootstrapProvisioningCommand request,
            CancellationToken cancellationToken)
        {
            RestartCityPopulationBootstrapResult restarted = await mediator.Send(
                request: new RestartCityPopulationBootstrapCommand(
                    CityId: request.CityId,
                    PlannedPeopleCountOverride: request.PlannedPeopleCountOverride),
                cancellationToken: cancellationToken);

            return restarted.Status switch
            {
                RestartCityPopulationBootstrapStatus.Restarted =>
                    RetryCityPopulationBootstrapProvisioningResult.Accepted(
                        await orchestrator.GetProvisioningViewAsync(
                            cityId: request.CityId,
                            cancellationToken: cancellationToken)),
                RestartCityPopulationBootstrapStatus.NotFound =>
                    RetryCityPopulationBootstrapProvisioningResult.NotFound(),
                RestartCityPopulationBootstrapStatus.NotAllowed =>
                    RetryCityPopulationBootstrapProvisioningResult.NotAllowed(),
                _ => throw new InvalidOperationException($"Unsupported retry provisioning status '{restarted.Status}'.")
            };
        }
    }
}
