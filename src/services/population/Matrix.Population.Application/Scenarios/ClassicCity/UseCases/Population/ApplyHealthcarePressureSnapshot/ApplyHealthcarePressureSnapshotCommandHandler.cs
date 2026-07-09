using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyHealthcarePressureSnapshot;

public sealed class ApplyHealthcarePressureSnapshotCommandHandler(
    ICityPopulationArchiveStateRepository archiveStateRepository,
    ICityPopulationDeletionStateRepository deletionStateRepository,
    ICityHealthcarePressureSnapshotRepository snapshotRepository,
    IProcessedIntegrationMessageRepository processedMessageRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApplyHealthcarePressureSnapshotCommand, ApplyHealthcarePressureSnapshotResult>
{
    public Task<ApplyHealthcarePressureSnapshotResult> Handle(
        ApplyHealthcarePressureSnapshotCommand request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var cityId = CityId.From(request.CityId);

        return unitOfWork.ExecuteInTransactionAsync(
            action: async ct =>
            {
                bool marked = await processedMessageRepository.TryMarkProcessedAsync(
                    consumer: request.ConsumerName,
                    messageId: request.IntegrationMessageId,
                    processedAtUtc: timeProvider.GetUtcNow(),
                    cancellationToken: ct);
                if (!marked)
                    return Result(ApplyHealthcarePressureSnapshotStatus.Duplicate);

                if (await deletionStateRepository.GetByCityAsync(cityId, ct) is not null)
                    return Result(ApplyHealthcarePressureSnapshotStatus.CityDeleted);
                if (await archiveStateRepository.GetByCityAsync(cityId, ct) is not null)
                    return Result(ApplyHealthcarePressureSnapshotStatus.CityArchived);

                ClassicCityHealthcarePressureSnapshot? existing =
                    await snapshotRepository.GetByCityAsync(cityId, ct);
                if (existing is not null && request.SourceRevision <= existing.SourceRevision)
                    return Result(ApplyHealthcarePressureSnapshotStatus.Stale);

                await snapshotRepository.UpsertAsync(
                    new ClassicCityHealthcarePressureSnapshot(
                        CityId: cityId,
                        SourceRevision: request.SourceRevision,
                        CurrentDate: request.CurrentDate,
                        PatientCount: request.PatientCount,
                        Pressure: new CityPopulationHealthcarePressureProfile(
                            ActiveIllnessCount: request.ActiveIllnessCount,
                            SevereIllnessCount: request.SevereIllnessCount,
                            MedicalLoadIndex: request.MedicalLoadIndex,
                            TriagePressureIndex: request.TriagePressureIndex,
                            RecoverySupportIndex: request.RecoverySupportIndex),
                        OccurredAtUtc: request.OccurredAtUtc,
                        UpdatedAtUtc: timeProvider.GetUtcNow(),
                        Districts: request.Districts
                           .Select(district => new ClassicCityHealthcareDistrictHealthSnapshot(
                                DistrictId: DistrictId.From(district.DistrictId),
                                PatientCount: district.PatientCount,
                                ActiveIllnessCount: district.ActiveIllnessCount,
                                SevereIllnessCount: district.SevereIllnessCount))
                           .ToArray()),
                    ct);
                await unitOfWork.SaveChangesAsync(ct);

                return Result(ApplyHealthcarePressureSnapshotStatus.Applied);
            },
            cancellationToken);
    }

    private static ApplyHealthcarePressureSnapshotResult Result(
        ApplyHealthcarePressureSnapshotStatus status) => new(status);

    private static void Validate(ApplyHealthcarePressureSnapshotCommand request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CityId == Guid.Empty)
            throw new ArgumentException("A city identifier is required.", nameof(request));
        if (request.IntegrationMessageId == Guid.Empty)
            throw new ArgumentException("An integration message identifier is required.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConsumerName);
        if (request.SourceRevision < 0)
            throw new ArgumentOutOfRangeException(nameof(request));
        if (request.PatientCount < 0
            || request.ActiveIllnessCount < 0
            || request.ActiveIllnessCount > request.PatientCount
            || request.SevereIllnessCount < 0
            || request.SevereIllnessCount > request.ActiveIllnessCount)
            throw new ArgumentException("Healthcare population counts are invalid.", nameof(request));
        if (request.MedicalLoadIndex is < 0.20m or > 3m
            || request.TriagePressureIndex is < 0m or > 3m
            || request.RecoverySupportIndex is < 0.25m or > 1.75m)
            throw new ArgumentException("Healthcare pressure indexes are invalid.", nameof(request));
        if (request.OccurredAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Healthcare snapshot timestamps must be expressed in UTC.", nameof(request));

        if (request.Districts.Any(district =>
                district.DistrictId == Guid.Empty
                || district.PatientCount < 0
                || district.ActiveIllnessCount < 0
                || district.ActiveIllnessCount > district.PatientCount
                || district.SevereIllnessCount < 0
                || district.SevereIllnessCount > district.ActiveIllnessCount))
            throw new ArgumentException("Healthcare district counts are invalid.", nameof(request));
        if (request.Districts.Select(district => district.DistrictId).Distinct().Count()
            != request.Districts.Count)
            throw new ArgumentException("Healthcare districts must be unique.", nameof(request));
        if (request.Districts.Sum(district => district.PatientCount) > request.PatientCount
            || request.Districts.Sum(district => district.ActiveIllnessCount) > request.ActiveIllnessCount
            || request.Districts.Sum(district => district.SevereIllnessCount) > request.SevereIllnessCount)
            throw new ArgumentException(
                "Healthcare district counts cannot exceed the population aggregate.",
                nameof(request));
    }
}
