using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyHealthcarePressureSnapshot;

public sealed record ApplyHealthcarePressureSnapshotCommand(
    Guid CityId,
    Guid IntegrationMessageId,
    string ConsumerName,
    long SourceRevision,
    DateOnly CurrentDate,
    int PatientCount,
    int ActiveIllnessCount,
    int SevereIllnessCount,
    decimal MedicalLoadIndex,
    decimal TriagePressureIndex,
    decimal RecoverySupportIndex,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyList<HealthcareDistrictHealthSnapshotInput>? Districts = null)
    : IRequest<ApplyHealthcarePressureSnapshotResult>
{
    public IReadOnlyList<HealthcareDistrictHealthSnapshotInput> Districts { get; init; } =
        Districts ?? [];
}

public sealed record HealthcareDistrictHealthSnapshotInput(
    Guid DistrictId,
    int PatientCount,
    int ActiveIllnessCount,
    int SevereIllnessCount);
