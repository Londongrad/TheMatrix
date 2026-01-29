using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Topology;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Topology.GetCityResidentialBuildings
{
    public sealed class GetCityResidentialBuildingsQueryHandler(IResidentialBuildingRepository repository)
        : IRequestHandler<GetCityResidentialBuildingsQuery, IReadOnlyList<ResidentialBuildingDto>>
    {
        public async Task<IReadOnlyList<ResidentialBuildingDto>> Handle(
            GetCityResidentialBuildingsQuery request,
            CancellationToken cancellationToken)
        {
            DistrictId? districtId = request.DistrictId.HasValue
                ? new DistrictId(request.DistrictId.Value)
                : null;

            return (await repository.ListByCityIdAsync(
                    cityId: new CityId(request.CityId),
                    districtId: districtId,
                    cancellationToken: cancellationToken))
               .Select(ResidentialBuildingDto.FromDomain)
               .ToArray();
        }
    }
}