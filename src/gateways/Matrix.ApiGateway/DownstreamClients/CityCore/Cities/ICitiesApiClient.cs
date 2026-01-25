using Matrix.CityCore.Contracts.Cities.Requests;
using Matrix.CityCore.Contracts.Cities.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Cities
{
    public interface ICitiesApiClient
    {
        Task<CityCreatedView> CreateCityAsync(
            CreateCityRequest request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CityListItemView>> ListCitiesAsync(
            bool includeArchived,
            CancellationToken cancellationToken = default);

        Task<CityView> GetCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task RenameCityAsync(
            Guid cityId,
            RenameCityRequest request,
            CancellationToken cancellationToken = default);

        Task ArchiveCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task DeleteCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
