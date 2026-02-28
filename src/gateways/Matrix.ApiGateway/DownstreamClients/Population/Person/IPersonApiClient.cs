using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.Person
{
    public interface IPersonApiClient
    {
        Task<PersonDto> KillAsync(
            Guid personId,
            CancellationToken cancellationToken = default);

        Task<PersonDto> ResurrectAsync(
            Guid personId,
            CancellationToken cancellationToken = default);
    }
}
