using Matrix.Population.Application.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Abstractions
{
    public interface ICityPopulationSummaryReadRepository
    {
        Task<CityPopulationSummaryReadModel?> GetByCityIdAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default);
    }
}
