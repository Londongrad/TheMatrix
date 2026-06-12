using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles
{
    public sealed class GetCityStockpilesQueryHandler(ICityStockpileRepository repository)
        : IRequestHandler<GetCityStockpilesQuery, CityStockpilesDto?>
    {
        public async Task<CityStockpilesDto?> Handle(
            GetCityStockpilesQuery request,
            CancellationToken cancellationToken)
        {
            SimulationHostId simulationHostId = new(request.CityId);

            CityStockpileState? state = await repository.GetBySimulationHostIdAsync(
                simulationHostId: simulationHostId,
                cancellationToken: cancellationToken);

            return state is null
                ? null
                : CityStockpilesDto.FromDomain(state);
        }
    }
}
