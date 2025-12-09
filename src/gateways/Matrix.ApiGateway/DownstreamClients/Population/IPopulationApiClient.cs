using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population
{
    public interface IPopulationApiClient
    {
        /// <summary>Инициализирует/пересоздаёт популяцию.</summary>
        Task InitializePopulationAsync(
            int peopleCount,
            int? randomSeed = null,
            CancellationToken cancellationToken = default);

        Task<PersonDto> KillPersonAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<PersonDto> ResurrectPersonAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<PersonDto> UpdatePersonAsync(
            Guid id,
            UpdatePersonRequest request,
            CancellationToken cancellationToken = default);

        Task<PagedResult<PersonDto>> GetCitizensPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
