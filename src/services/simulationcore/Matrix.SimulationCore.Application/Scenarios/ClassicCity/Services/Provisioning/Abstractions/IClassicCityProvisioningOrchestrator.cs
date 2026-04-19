using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using CityProvisioningView = Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning.CityProvisioningModel;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions
{
    public interface IClassicCityProvisioningOrchestrator
    {
        Task<CityProvisioningView> CreateAsync(
            CreateCityCommand request,
            CancellationToken cancellationToken);

        Task<CityProvisioningView> GetProvisioningViewAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task<CityProvisioningView> ProvisionAsync(
            Guid cityId,
            string simulationKind,
            Guid populationBootstrapOperationId,
            Guid economyBootstrapOperationId,
            int? plannedPeopleCountOverride,
            Func<CancellationToken, Task>? heartbeatAsync,
            CancellationToken cancellationToken);
    }
}
