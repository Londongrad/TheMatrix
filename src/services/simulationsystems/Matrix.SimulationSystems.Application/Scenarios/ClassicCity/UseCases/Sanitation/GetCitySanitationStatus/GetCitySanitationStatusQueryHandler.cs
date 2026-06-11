using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus
{
    public sealed class GetCitySanitationStatusQueryHandler(
        ICityEnvironmentalConditionRepository repository,
        ClassicCityWeatherPressureProfileFactory pressureProfileFactory)
        : IRequestHandler<GetCitySanitationStatusQuery, CitySanitationStatusDto?>
    {
        public async Task<CitySanitationStatusDto?> Handle(
            GetCitySanitationStatusQuery request,
            CancellationToken cancellationToken)
        {
            var simulationHostId = new SimulationHostId(request.CityId);

            CityEnvironmentalConditionState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            decimal sanitationSupport = pressureProfileFactory.Create(state)
               .SanitationSupport;

            return CitySanitationStatusDto.FromState(
                cityId: request.CityId,
                state: state,
                sanitationSupportIndex: sanitationSupport);
        }
    }
}
