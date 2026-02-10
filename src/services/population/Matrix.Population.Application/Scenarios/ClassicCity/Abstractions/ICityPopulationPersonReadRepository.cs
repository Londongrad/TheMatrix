using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationPersonReadRepository
    {
        Task<IReadOnlyCollection<Person>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);
    }
}
