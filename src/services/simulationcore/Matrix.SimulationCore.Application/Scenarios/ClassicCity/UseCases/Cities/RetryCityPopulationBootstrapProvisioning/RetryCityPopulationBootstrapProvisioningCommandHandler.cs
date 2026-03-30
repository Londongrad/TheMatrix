using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RetryCityPopulationBootstrapProvisioning
{
    public sealed class RetryCityPopulationBootstrapProvisioningCommandHandler(
        IMediator mediator,
        IClassicCityProvisioningOrchestrator orchestrator)
        : IRequestHandler<RetryCityPopulationBootstrapProvisioningCommand, RetryCityPopulationBootstrapProvisioningResult>
    {
        public async Task<RetryCityPopulationBootstrapProvisioningResult> Handle(
            RetryCityPopulationBootstrapProvisioningCommand request,
            CancellationToken cancellationToken)
        {
            RestartCityPopulationBootstrapResult restarted = await mediator.Send(
                request: new RestartCityPopulationBootstrapCommand(CityId: request.CityId),
                cancellationToken: cancellationToken);

            return restarted.Status switch
            {
                RestartCityPopulationBootstrapStatus.Restarted =>
                    RetryCityPopulationBootstrapProvisioningResult.Provisioned(
                        await orchestrator.ProvisionAsync(
                            cityId: request.CityId,
                            simulationKind: restarted.SimulationKind!,
                            populationBootstrapOperationId: restarted.PopulationBootstrapOperationId!.Value,
                            economyBootstrapOperationId: restarted.EconomyBootstrapOperationId!.Value,
                            plannedPeopleCountOverride: request.PlannedPeopleCountOverride,
                            cancellationToken: cancellationToken)),
                RestartCityPopulationBootstrapStatus.NotFound =>
                    RetryCityPopulationBootstrapProvisioningResult.NotFound(),
                RestartCityPopulationBootstrapStatus.NotAllowed =>
                    RetryCityPopulationBootstrapProvisioningResult.NotAllowed(),
                _ => throw new InvalidOperationException(
                    $"Unsupported retry provisioning status '{restarted.Status}'.")
            };
        }
    }
}
