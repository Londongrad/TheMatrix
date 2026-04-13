using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions
{
    public interface ICityRouteResolutionClient
    {
        Task<CityPopulationCommuteContext?> ResolveResidentialToAnchorAsync(
            Guid cityId,
            ResidentialBuildingId residentialBuildingId,
            CityAnchorId cityAnchorId,
            string profile,
            CancellationToken cancellationToken);
    }
}
