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
    DateTimeOffset OccurredAtUtc)
    : IRequest<ApplyHealthcarePressureSnapshotResult>;
