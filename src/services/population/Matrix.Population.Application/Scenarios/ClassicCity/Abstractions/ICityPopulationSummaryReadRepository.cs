using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationSummaryReadRepository
    {
        Task<CityPopulationSummaryReadModel?> GetByCityIdAsync(
            CityId cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default);
    }
}
