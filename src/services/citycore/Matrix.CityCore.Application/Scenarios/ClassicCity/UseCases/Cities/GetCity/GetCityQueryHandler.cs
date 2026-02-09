using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity
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
