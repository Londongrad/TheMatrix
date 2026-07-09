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

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes
{
    public sealed class ApplyPatientHealthOutcomesCommandHandler(
        ICityPopulationPersonReadRepository personReadRepository,
        ICityPopulationArchiveStateRepository archiveStateRepository,
        ICityPopulationDeletionStateRepository deletionStateRepository,
        IProcessedIntegrationMessageRepository processedMessageRepository,
        IPopulationResidentFactsOutboxWriter residentFactsOutboxWriter,
        MarriageDomainService marriageDomainService,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyPatientHealthOutcomesCommand, ApplyPatientHealthOutcomesResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<ApplyPatientHealthOutcomesResult> Handle(
            ApplyPatientHealthOutcomesCommand request,
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
                        return Result(ApplyPatientHealthOutcomesStatus.Duplicate);

                    if (await deletionStateRepository.GetByCityAsync(cityId, ct) is not null)
                        return Result(ApplyPatientHealthOutcomesStatus.CityDeleted);
                    if (await archiveStateRepository.GetByCityAsync(cityId, ct) is not null)
                        return Result(ApplyPatientHealthOutcomesStatus.CityArchived);

                    PersonId[] patientIds = request.Patients
                       .Select(patient => PersonId.From(patient.PatientId))
                       .ToArray();
                    IReadOnlyCollection<Person> patients = await personReadRepository.ListByCityAndIdsAsync(
                        cityId,
                        patientIds,
                        ct);
                    Dictionary<PersonId, Person> residentsById = patients.ToDictionary(person => person.Id);
                    PersonId[] spouseIds = patients
                       .Where(patient => patient.SpouseId.HasValue)
                       .Select(patient => patient.SpouseId!.Value)
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
                    foreach (PatientHealthOutcomeInput outcome in request.Patients)
                    {
                        var patientId = PersonId.From(outcome.PatientId);
                        if (!residentsById.TryGetValue(patientId, out Person? patient))
                            continue;

                        bool wasAlive = patient.IsAlive;
                        bool accepted = patient.TryApplyHealthcareOutcome(
                            sourceRevision: request.SourceRevision,
                            healthScore: outcome.HealthScore,
                            illness: patient.Illness,
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
                        if (wasAlive && !patient.IsAlive)
                        {
                            lifecycleChanges.Add(patient);
                            ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                                deceased: patient,
                                residentsById: residentsById,
                                marriageDomainService: marriageDomainService);
                        }
                    }

                    foreach (PopulationResidentFactsBatchV1 batch in PopulationResidentFactsBatchFactory.Build(
                                 simulationHostId: request.CityId,
                                 sourceRevision: request.SourceRevision,
                                 residents: lifecycleChanges,
                                 correlationId:
                                 $"population:{request.CityId:N}:healthcare:{request.SourceRevision}:resident-facts",
                                 synchronizedAtUtc: request.OccurredAtUtc))
                        await residentFactsOutboxWriter.AddResidentFactsBatchAsync(batch, ct);

                    await unitOfWork.SaveChangesAsync(ct);
                    return new ApplyPatientHealthOutcomesResult(
                        Status: ApplyPatientHealthOutcomesStatus.Applied,
                        AppliedPatientCount: applied,
                        IgnoredPatientCount: request.Patients.Count - applied - stale,
                        StalePatientCount: stale);
                },
                cancellationToken: cancellationToken);
        }

        private static ApplyPatientHealthOutcomesResult Result(ApplyPatientHealthOutcomesStatus status) =>
            new(status, 0, 0, 0);

        private static void Validate(ApplyPatientHealthOutcomesCommand request)
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
            ArgumentNullException.ThrowIfNull(request.Patients);
            if (request.Patients.Count > MaxBatchSize)
                throw new ArgumentException($"Outcome batches cannot exceed {MaxBatchSize} patients.", nameof(request));
            if (request.Patients.Any(patient => patient.PatientId == Guid.Empty
                                                || patient.HealthScore is < 0 or > 100
                                                || patient.FunctionalCapacityScore is < 0 or > 100
                                                || patient.LifecycleRevision < 0))
                throw new ArgumentException("Outcome patient data is invalid.", nameof(request));
            if (request.Patients.Select(patient => patient.PatientId).Distinct().Count() != request.Patients.Count)
                throw new ArgumentException("Outcome batches cannot contain duplicate patients.", nameof(request));
        }
    }
}
