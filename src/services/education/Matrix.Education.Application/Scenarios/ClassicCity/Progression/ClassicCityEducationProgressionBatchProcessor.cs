using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Integration;
using Matrix.Education.Application.Progression;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Students;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Progression
{
    public sealed class ClassicCityEducationProgressionBatchProcessor(
        IStudentProfileRepository studentProfileRepository,
        IStudentEnrollmentRepository enrollmentRepository,
        IEducationInstitutionRepository institutionRepository,
        ClassicCityEducationProgressionPolicy progressionPolicy,
        ClassicCityEducationInstitutionSelectionPolicy institutionSelectionPolicy)
        : IEducationProgressionBatchProcessor
    {
        public SimulationRuntimeKey RuntimeKey { get; } = new(
            scenarioKey: new SimulationScenarioKey("classic-city"),
            hostTypeKey: new SimulationHostTypeKey("city"));

        public async Task<EducationProgressionBatchResult> ProcessAsync(
            EducationProgressionBatch batch,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(batch);
            if (batch.RuntimeKey != RuntimeKey)
                throw new ArgumentException(
                    $"Classic City education cannot process runtime '{batch.RuntimeKey}'.",
                    nameof(batch));

            IReadOnlyList<StudentProfile> profiles =
                await studentProfileRepository.ListBySimulationHostAsync(
                    batch.SimulationHostId,
                    cancellationToken);
            IReadOnlyList<StudentEnrollment> activeEnrollments =
                await enrollmentRepository.ListActiveAsync(
                    batch.SimulationHostId,
                    cancellationToken);
            IReadOnlyList<EducationInstitution> institutions =
                await institutionRepository.ListAsync(
                    batch.SimulationHostId,
                    cancellationToken);

            Dictionary<ResidentId, StudentProfile> profilesById = profiles.ToDictionary(
                profile => profile.ResidentId);
            Dictionary<ResidentId, StudentEnrollment> activeByResident = activeEnrollments.ToDictionary(
                enrollment => enrollment.ResidentId);
            Dictionary<EducationInstitutionId, EducationInstitution> institutionsById = institutions.ToDictionary(
                institution => institution.EducationInstitutionId);
            var updatedInstitutionIds = new HashSet<EducationInstitutionId>();
            var changedResidentIds = new HashSet<ResidentId>();
            var addedEnrollments = new List<StudentEnrollment>();
            DateOnly currentDate = DateOnly.FromDateTime(batch.ToSimTimeUtc.UtcDateTime);
            int completedCount = 0;
            int withdrawnCount = 0;

            foreach (StudentEnrollment enrollment in activeEnrollments)
            {
                if (!profilesById.TryGetValue(enrollment.ResidentId, out StudentProfile? profile))
                    throw new InvalidOperationException(
                        $"Active enrollment '{enrollment.EnrollmentId}' has no profile in simulation '{batch.SimulationHostId}'.");

                EducationInstitution institution = ResolveInstitution(
                    enrollment,
                    institutionsById,
                    batch);
                if (!profile.IsAlive || !profile.IsActive)
                {
                    DateOnly withdrawnOn = currentDate < enrollment.EnrolledOn
                        ? enrollment.EnrolledOn
                        : currentDate;
                    enrollment.Withdraw(withdrawnOn);
                    institution.ReleaseSeats(1);
                    updatedInstitutionIds.Add(institution.EducationInstitutionId);
                    activeByResident.Remove(enrollment.ResidentId);
                    changedResidentIds.Add(enrollment.ResidentId);
                    withdrawnCount++;
                    continue;
                }

                DateOnly? completionDate = progressionPolicy.ResolveCompletionDate(
                    profile,
                    enrollment.Stage,
                    enrollment.EnrolledOn);
                if (!completionDate.HasValue || completionDate.Value > currentDate)
                    continue;

                enrollment.Complete(completionDate.Value);
                profile.RecordStageCompletion(enrollment.Stage, completionDate.Value);
                institution.ReleaseSeats(1);
                updatedInstitutionIds.Add(institution.EducationInstitutionId);
                activeByResident.Remove(enrollment.ResidentId);
                changedResidentIds.Add(enrollment.ResidentId);
                completedCount++;
            }

            foreach (StudentProfile profile in profiles)
            {
                if (!profile.IsAlive || !profile.IsActive || activeByResident.ContainsKey(profile.ResidentId))
                    continue;

                if (progressionPolicy.TryResolveInferredBaseline(
                        profile,
                        currentDate,
                        out EducationStageKey baselineStage,
                        out DateOnly baselineCompletedOn))
                {
                    profile.RecordStageCompletion(baselineStage, baselineCompletedOn);
                    changedResidentIds.Add(profile.ResidentId);
                }

                EducationStageKey? nextStage = progressionPolicy.ResolveNextEnrollmentStage(
                    profile,
                    currentDate);
                if (!nextStage.HasValue)
                    continue;

                EducationInstitution? institution = institutionSelectionPolicy.TryReserveInstitution(
                    profile.ResidentId,
                    nextStage.Value,
                    institutions);
                if (institution is null)
                    continue;

                var enrollment = StudentEnrollment.Enroll(
                    id: ProgressionEnrollmentIdFactory.Create(
                        batch.SimulationHostId,
                        profile.ResidentId,
                        nextStage.Value,
                        currentDate,
                        batch.TickId),
                    simulationHostId: batch.SimulationHostId,
                    residentId: profile.ResidentId,
                    institutionId: institution.EducationInstitutionId,
                    stage: nextStage.Value,
                    enrolledOn: currentDate);
                addedEnrollments.Add(enrollment);
                activeByResident.Add(profile.ResidentId, enrollment);
                updatedInstitutionIds.Add(institution.EducationInstitutionId);
                changedResidentIds.Add(profile.ResidentId);
            }

            if (addedEnrollments.Count > 0)
                await enrollmentRepository.AddRangeAsync(
                    addedEnrollments,
                    cancellationToken);

            EducationStudentParticipationChange[] participationChanges = changedResidentIds
               .OrderBy(residentId => residentId.Value)
               .Select(residentId =>
                {
                    StudentProfile profile = profilesById[residentId];
                    profile.RecordParticipationChange();
                    activeByResident.TryGetValue(
                        residentId,
                        out StudentEnrollment? activeEnrollment);
                    EducationInstitution? institution = activeEnrollment is null
                        ? null
                        : ResolveInstitution(activeEnrollment, institutionsById, batch);
                    return EducationStudentParticipationChange.Capture(
                        profile,
                        activeEnrollment,
                        institution);
                })
               .ToArray();

            return new EducationProgressionBatchResult(
                StudentProfilesEvaluated: profiles.Count,
                EnrollmentsStarted: addedEnrollments.Count,
                EnrollmentsCompleted: completedCount,
                EnrollmentsWithdrawn: withdrawnCount,
                InstitutionsUpdated: updatedInstitutionIds.Count,
                ParticipationChanges: participationChanges);
        }

        private static EducationInstitution ResolveInstitution(
            StudentEnrollment enrollment,
            IReadOnlyDictionary<EducationInstitutionId, EducationInstitution> institutionsById,
            EducationProgressionBatch batch)
        {
            return institutionsById.TryGetValue(
                enrollment.InstitutionId,
                out EducationInstitution? institution)
                ? institution
                : throw new InvalidOperationException(
                    $"Active enrollment '{enrollment.EnrollmentId}' has no institution in simulation '{batch.SimulationHostId}'.");
        }
    }
}
