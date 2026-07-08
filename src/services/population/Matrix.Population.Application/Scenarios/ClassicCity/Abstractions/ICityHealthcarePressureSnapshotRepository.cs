using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;

public interface ICityHealthcarePressureSnapshotRepository
{
    Task<ClassicCityHealthcarePressureSnapshot?> GetByCityAsync(
        CityId cityId,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        ClassicCityHealthcarePressureSnapshot snapshot,
        CancellationToken cancellationToken = default);

    Task DeleteByCityAsync(
        CityId cityId,
        CancellationToken cancellationToken = default);
}
