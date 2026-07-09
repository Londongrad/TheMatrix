using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Contracts.Events;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyResidentVitalStateOutcomes
{
    public sealed class ApplyResidentVitalStateOutcomesCommandHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        ICityPopulationArchiveStateRepository archiveStateRepository,
        ICityPopulationDeletionStateRepository deletionStateRepository,
        IProcessedIntegrationMessageRepository processedMessageRepository,
        IPopulationResidentFactsOutboxWriter residentFactsOutboxWriter,
        MarriageDomainService marriageDomainService,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyResidentVitalStateOutcomesCommand, ApplyResidentVitalStateOutcomesResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<ApplyResidentVitalStateOutcomesResult> Handle(
            ApplyResidentVitalStateOutcomesCommand request,
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
                        return Result(ApplyResidentVitalStateOutcomesStatus.Duplicate);

                    if (await deletionStateRepository.GetByCityAsync(cityId, ct) is not null)
                        return Result(ApplyResidentVitalStateOutcomesStatus.CityDeleted);
                    if (await archiveStateRepository.GetByCityAsync(cityId, ct) is not null)
                        return Result(ApplyResidentVitalStateOutcomesStatus.CityArchived);

                    PersonId[] residentIds = request.Residents
                       .Select(resident => PersonId.From(resident.ResidentId))
                       .ToArray();
                    IReadOnlyCollection<Person> residents = await personReadRepository.ListByCityAndIdsAsync(
                        cityId,
                        residentIds,
                        ct);
                    Dictionary<PersonId, Person> residentsById = residents.ToDictionary(person => person.Id);
                    PersonId[] spouseIds = residents
                       .Where(resident => resident.SpouseId.HasValue)
                       .Select(resident => resident.SpouseId!.Value)
                       .Where(spouseId => !residentsById.ContainsKey(spouseId))
                       .Distinct()
                       .ToArray();
                    if (spouseIds.Length > 0)
                        foreach (Person spouse in await personReadRepository.ListByCityAndIdsAsync(
                                     cityId,
                                     spouseIds,
                                     ct))
                            residentsById[spouse.Id] = spouse;

                    int applied = 0;
                    int stale = 0;
                    List<Person> lifecycleChanges = [];
                    foreach (ResidentVitalStateOutcomeInput outcome in request.Residents)
                    {
                        var residentId = PersonId.From(outcome.ResidentId);
                        if (!residentsById.TryGetValue(residentId, out Person? resident))
                            continue;

                        bool wasAlive = resident.IsAlive;
                        bool accepted = resident.TryApplyVitalStateProjection(
                            sourceRevision: request.SourceRevision,
                            healthScore: outcome.HealthScore,
                            happinessDelta: outcome.HappinessDelta,
                            energyDelta: outcome.EnergyDelta,
                            stressDelta: outcome.StressDelta,
                            currentDate: request.CurrentDate,
                            expectedLifecycleRevision: outcome.LifecycleRevision,
                            functionalCapacityScore: outcome.FunctionalCapacityScore);
                        if (!accepted)
                        {
                            stale++;
                            continue;
                        }

                        applied++;
                        if (wasAlive && !resident.IsAlive)
                        {
                            lifecycleChanges.Add(resident);
                            ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                                deceased: resident,
                                residentsById: residentsById,
                                marriageDomainService: marriageDomainService);
                        }
                    }

                    foreach (PopulationResidentFactsBatchV1 batch in PopulationResidentFactsBatchFactory.Build(
                                 simulationHostId: request.CityId,
                                 sourceRevision: request.SourceRevision,
                                 residents: lifecycleChanges,
                                 correlationId: $"{request.CorrelationId}:resident-facts",
                                 synchronizedAtUtc: request.OccurredAtUtc))
                        await residentFactsOutboxWriter.AddResidentFactsBatchAsync(batch, ct);

                    await unitOfWork.SaveChangesAsync(ct);
                    return new ApplyResidentVitalStateOutcomesResult(
                        Status: ApplyResidentVitalStateOutcomesStatus.Applied,
                        AppliedResidentCount: applied,
                        IgnoredResidentCount: request.Residents.Count - applied - stale,
                        StaleResidentCount: stale);
                },
                cancellationToken: cancellationToken);
        }

        private static ApplyResidentVitalStateOutcomesResult Result(ApplyResidentVitalStateOutcomesStatus status) =>
            new(status, 0, 0, 0);

        private static void Validate(ApplyResidentVitalStateOutcomesCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.CityId == Guid.Empty)
                throw new ArgumentException("A city identifier is required.", nameof(request));
            if (request.IntegrationMessageId == Guid.Empty)
                throw new ArgumentException("An integration message identifier is required.", nameof(request));
            ArgumentException.ThrowIfNullOrWhiteSpace(request.ConsumerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.CorrelationId);
            if (request.SourceRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(request));
            if (request.OccurredAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("Outcome timestamps must be expressed in UTC.", nameof(request));
            if (request.TotalBatches <= 0
                || request.BatchNumber <= 0
                || request.BatchNumber > request.TotalBatches)
                throw new ArgumentException("Outcome batch position metadata is invalid.", nameof(request));
            ArgumentNullException.ThrowIfNull(request.Residents);
            if (request.Residents.Count > MaxBatchSize)
                throw new ArgumentException($"Outcome batches cannot exceed {MaxBatchSize} residents.", nameof(request));
            if (request.Residents.Any(resident => resident.ResidentId == Guid.Empty
                                                  || resident.HealthScore is < 0 or > 100
                                                  || resident.FunctionalCapacityScore is < 0 or > 100
                                                  || resident.LifecycleRevision < 0))
                throw new ArgumentException("Outcome resident data is invalid.", nameof(request));
            if (request.Residents.Select(resident => resident.ResidentId).Distinct().Count()
                != request.Residents.Count)
                throw new ArgumentException("Outcome batches cannot contain duplicate residents.", nameof(request));
        }
    }
}
