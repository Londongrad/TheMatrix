using MediatR;

namespace Matrix.CityCore.Application.UseCases.Topology.GetCityResidentialBuildings
{
    public sealed record GetCityResidentialBuildingsQuery(
        Guid CityId,
        Guid? DistrictId) : IRequest<IReadOnlyList<ResidentialBuildingDto>>;
}