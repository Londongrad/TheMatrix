using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions
{
    public interface IClassicCityProvisioningOrchestrator
    {
        Task<CityProvisioningView> CreateAsync(
            CreateCityCommand request,
            CancellationToken cancellationToken);

        Task<CityProvisioningView> ProvisionAsync(
            Guid cityId,
            string simulationKind,
            Guid populationBootstrapOperationId,
            Guid economyBootstrapOperationId,
            int? plannedPeopleCountOverride,
            CancellationToken cancellationToken);
    }
}
