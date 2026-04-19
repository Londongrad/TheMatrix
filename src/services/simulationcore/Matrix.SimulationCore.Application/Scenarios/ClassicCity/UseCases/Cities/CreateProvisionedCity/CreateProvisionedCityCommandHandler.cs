using CityProvisioningView = Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning.CityProvisioningModel;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateProvisionedCity
{
    public sealed class CreateProvisionedCityCommandHandler(IClassicCityProvisioningOrchestrator orchestrator)
        : IRequestHandler<CreateProvisionedCityCommand, CityProvisioningView>
    {
        public Task<CityProvisioningView> Handle(
            CreateProvisionedCityCommand request,
            CancellationToken cancellationToken)
        {
            return orchestrator.CreateAsync(
                request: request.City,
                cancellationToken: cancellationToken);
        }
    }
}
