using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions
{
    public sealed record CityRouteResolutionBatchRequestItem(
        ResidentialBuildingId ResidentialBuildingId,
        CityAnchorId CityAnchorId,
        string Profile);

    public interface ICityRouteResolutionClient
    {
        Task<CityPopulationCommuteContext?> ResolveResidentialToAnchorAsync(
            Guid cityId,
            ResidentialBuildingId residentialBuildingId,
            CityAnchorId cityAnchorId,
            string profile,
            CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<CityRouteResolutionBatchRequestItem, CityPopulationCommuteContext?>>
            ResolveResidentialToAnchorsAsync(
                Guid cityId,
                IReadOnlyCollection<CityRouteResolutionBatchRequestItem> requests,
                CancellationToken cancellationToken);
    }
}
