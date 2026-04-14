using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityDistrictUtilityConditionsClient
    {
        Task<IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken);
    }
}
