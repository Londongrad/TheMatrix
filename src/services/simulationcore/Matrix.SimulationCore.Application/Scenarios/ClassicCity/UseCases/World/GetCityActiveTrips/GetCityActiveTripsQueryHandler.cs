using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.GetCityActiveTrips
{
    public sealed class GetCityActiveTripsQueryHandler(ICityActiveTripRepository tripRepository)
        : IRequestHandler<GetCityActiveTripsQuery, IReadOnlyList<CityActiveTripDto>>
    {
        public async Task<IReadOnlyList<CityActiveTripDto>> Handle(
            GetCityActiveTripsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityActiveTrip> activeTrips =
                await tripRepository.ListActiveByCityIdAsync(
                    cityId: new CityId(request.CityId),
                    cancellationToken: cancellationToken);

            return activeTrips
               .Select(CityActiveTripMappings.ToDto)
               .ToArray();
        }
    }
}
