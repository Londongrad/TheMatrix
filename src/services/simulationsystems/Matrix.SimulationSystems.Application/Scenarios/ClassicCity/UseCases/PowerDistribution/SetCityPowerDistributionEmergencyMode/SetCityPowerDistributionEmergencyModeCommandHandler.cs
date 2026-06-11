using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    SetCityPowerDistributionEmergencyMode
{
    public sealed class SetCityPowerDistributionEmergencyModeCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<SetCityPowerDistributionEmergencyModeCommand, CityPowerDistributionStatusDto?>
    {
        public async Task<CityPowerDistributionStatusDto?> Handle(
            SetCityPowerDistributionEmergencyModeCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            state.SetPowerDistributionEmergencyMode(request.Enabled);

            CityEnvironmentalConditionSnapshot refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal powerSupport = pressureProfileFactory.Create(state)
               .PowerSupport;

            return CityPowerDistributionStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                powerSupportIndex: powerSupport);
        }
    }
}
