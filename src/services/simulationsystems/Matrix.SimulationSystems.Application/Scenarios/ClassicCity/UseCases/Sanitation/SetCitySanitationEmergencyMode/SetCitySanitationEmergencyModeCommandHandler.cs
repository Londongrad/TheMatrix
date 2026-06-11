using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.SetCitySanitationEmergencyMode
{
    public sealed class SetCitySanitationEmergencyModeCommandHandler(
        ICityEnvironmentalConditionRepository repository,
        IUnitOfWork unitOfWork,
        CityEnvironmentalConditionPolicy policy,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<SetCitySanitationEmergencyModeCommand, CitySanitationStatusDto?>
    {
        public async Task<CitySanitationStatusDto?> Handle(
            SetCitySanitationEmergencyModeCommand request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            state.SetSanitationEmergencyMode(request.Enabled);

            CityEnvironmentalConditionSnapshot refreshedSnapshot = policy.Recalculate(
                state: state,
                pressure: pressureProfileFactory.Create(state),
                asOfUtc: state.LastEvaluatedAtUtc);

            state.ApplySnapshot(refreshedSnapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            decimal sanitationSupport = pressureProfileFactory.Create(state)
               .SanitationSupport;

            return CitySanitationStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                sanitationSupportIndex: sanitationSupport);
        }
    }
}
