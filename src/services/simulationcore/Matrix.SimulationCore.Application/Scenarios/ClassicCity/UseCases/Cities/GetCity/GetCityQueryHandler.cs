using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity
{
    public sealed class GetCityQueryHandler(ICityRepository cityRepository)
        : IRequestHandler<GetCityQuery, CityDto?>
    {
        public async Task<CityDto?> Handle(
            GetCityQuery request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            return city is null
                ? null
                : CityDto.FromDomain(city);
        }
    }
}
