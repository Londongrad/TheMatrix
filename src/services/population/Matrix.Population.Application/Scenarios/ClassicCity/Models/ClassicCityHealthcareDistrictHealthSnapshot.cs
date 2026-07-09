using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models;

public sealed record ClassicCityHealthcareDistrictHealthSnapshot(
    DistrictId DistrictId,
    int PatientCount,
    int ActiveIllnessCount,
    int SevereIllnessCount);
