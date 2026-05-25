using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions
{
    public interface ICityPopulationCommuteRoutingService
    {
        Task PreloadAnchorCommutesAsync(
            Guid cityId,
            IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
            CancellationToken cancellationToken);

        Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? destinationAnchorId,
            CancellationToken cancellationToken);

        Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken);

        Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken);

        Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? healthcareAnchorId,
            CancellationToken cancellationToken);
    }
}
