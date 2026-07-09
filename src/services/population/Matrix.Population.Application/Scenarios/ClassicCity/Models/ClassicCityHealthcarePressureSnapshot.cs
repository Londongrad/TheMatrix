using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models;

public sealed record ClassicCityHealthcarePressureSnapshot(
    CityId CityId,
    long SourceRevision,
    DateOnly CurrentDate,
    int PatientCount,
    CityPopulationHealthcarePressureProfile Pressure,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ClassicCityHealthcareDistrictHealthSnapshot>? Districts = null)
{
    public IReadOnlyList<ClassicCityHealthcareDistrictHealthSnapshot> Districts { get; init; } =
        Districts ?? [];
}
