using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Integration.Education.ApplyEducationParticipation
{
    public sealed class ApplyEducationParticipationCommandHandler(
        IPersonReadRepository personReadRepository,
        IEducationParticipationProjectionRepository projectionRepository,
        IProcessedIntegrationMessageRepository processedMessageRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<ApplyEducationParticipationCommand, ApplyEducationParticipationResult>
    {
        public const int MaxBatchSize = 1000;

        public Task<ApplyEducationParticipationResult> Handle(
            ApplyEducationParticipationCommand request,
            CancellationToken cancellationToken)
        {
            Validate(request);

            return unitOfWork.ExecuteInTransactionAsync(
                action: token => ApplyInsideTransactionAsync(request, token),
                cancellationToken: cancellationToken);
        }

        private async Task<ApplyEducationParticipationResult> ApplyInsideTransactionAsync(
            ApplyEducationParticipationCommand request,
            CancellationToken cancellationToken)
        {
            bool marked = await processedMessageRepository.TryMarkProcessedAsync(
                consumer: request.ConsumerName,
                messageId: request.IntegrationMessageId,
                processedAtUtc: timeProvider.GetUtcNow(),
                cancellationToken: cancellationToken);
            if (!marked)
                return new ApplyEducationParticipationResult(
                    Status: ApplyEducationParticipationStatus.Duplicate);

            PersonId[] residentIds = request.Students
               .Select(student => PersonId.From(student.ResidentId))
               .ToArray();
            IReadOnlyCollection<Person> residents = await personReadRepository.GetByIdsAsync(
                residentIds,
                cancellationToken);
            Dictionary<PersonId, Person> residentsById = residents.ToDictionary(
                resident => resident.Id);
            DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();
            var projections = new List<EducationParticipationProjection>(request.Students.Count);
            int missingOrChangedResidentCount = 0;

            foreach (StudentEducationParticipationInput student in request.Students)
            {
                var residentId = PersonId.From(student.ResidentId);
                if (!residentsById.TryGetValue(residentId, out Person? resident)
                    || resident.LifecycleRevision != student.ResidentLifecycleRevision)
                {
                    missingOrChangedResidentCount++;
                    continue;
                }

                projections.Add(new EducationParticipationProjection(
                    SimulationHostId: request.SimulationHostId,
                    ResidentId: student.ResidentId,
                    ParticipationRevision: student.ParticipationRevision,
                    ResidentLifecycleRevision: student.ResidentLifecycleRevision,
                    IsEnrolled: student.IsEnrolled,
                    ActiveStage: student.ActiveStage,
                    InstitutionId: student.InstitutionId,
                    InstitutionAnchorId: student.InstitutionAnchorId,
                    EnrolledOn: student.EnrolledOn,
                    CompletedStage: student.CompletedStage,
                    CompletedStageOn: student.CompletedStageOn,
                    SnapshotDate: request.SnapshotDate,
                    OccurredAtUtc: request.OccurredAtUtc,
                    UpdatedAtUtc: updatedAtUtc));
            }

            int appliedStudentCount = projections.Count == 0
                ? 0
                : await projectionRepository.UpsertNewerAsync(
                    projections,
                    cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ApplyEducationParticipationResult(
                Status: ApplyEducationParticipationStatus.Applied,
                AppliedStudentCount: appliedStudentCount,
                StaleStudentCount: projections.Count - appliedStudentCount,
                MissingOrChangedResidentCount: missingOrChangedResidentCount);
        }

        private static void Validate(ApplyEducationParticipationCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Students);

            if (request.SimulationHostId == Guid.Empty)
                throw new ArgumentException(
                    message: "A simulation host identifier is required.",
                    paramName: nameof(request));
            if (request.IntegrationMessageId == Guid.Empty)
                throw new ArgumentException(
                    message: "An integration message identifier is required.",
                    paramName: nameof(request));
            ArgumentException.ThrowIfNullOrWhiteSpace(request.ConsumerName);
            if (request.OccurredAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Education participation timestamps must be expressed in UTC.",
                    paramName: nameof(request));
            if (request.BatchNumber <= 0
                || request.TotalBatches <= 0
                || request.BatchNumber > request.TotalBatches)
                throw new ArgumentException(
                    message: "Education participation batch position metadata is invalid.",
                    paramName: nameof(request));
            if (request.Students.Count == 0 || request.Students.Count > MaxBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(request),
                    message: $"Education participation batches must contain between 1 and {MaxBatchSize} students.");
            if (request.Students.Select(student => student.ResidentId).Distinct().Count()
                != request.Students.Count)
                throw new ArgumentException(
                    message: "Education participation students must be unique within a batch.",
                    paramName: nameof(request));

            foreach (StudentEducationParticipationInput student in request.Students)
                Validate(student, request.SnapshotDate, request);
        }

        private static void Validate(
            StudentEducationParticipationInput student,
            DateOnly snapshotDate,
            ApplyEducationParticipationCommand request)
        {
            if (student is null
                || student.ResidentId == Guid.Empty
                || student.ParticipationRevision <= 0
                || student.ResidentLifecycleRevision < 0)
                throw new ArgumentException(
                    message: "Education participation student identity and revisions are invalid.",
                    paramName: nameof(request));

            bool hasCompleteEnrollment = !string.IsNullOrWhiteSpace(student.ActiveStage)
                                         && student.InstitutionId is { } institutionId
                                         && institutionId != Guid.Empty
                                         && student.EnrolledOn.HasValue;
            bool hasAnyEnrollment = !string.IsNullOrWhiteSpace(student.ActiveStage)
                                    || student.InstitutionId.HasValue
                                    || student.InstitutionAnchorId.HasValue
                                    || student.EnrolledOn.HasValue;
            bool hasCompletedStage = !string.IsNullOrWhiteSpace(student.CompletedStage);
            if (student.IsEnrolled != hasCompleteEnrollment
                || (!student.IsEnrolled && hasAnyEnrollment)
                || hasCompletedStage != student.CompletedStageOn.HasValue
                || student.EnrolledOn > snapshotDate
                || student.CompletedStageOn > snapshotDate)
                throw new ArgumentException(
                    message: "Education participation state is inconsistent.",
                    paramName: nameof(request));
        }
    }
}
