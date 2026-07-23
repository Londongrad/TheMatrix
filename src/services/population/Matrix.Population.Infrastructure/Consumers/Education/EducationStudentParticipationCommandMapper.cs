using Matrix.Education.Contracts.Events;
using Matrix.Population.Application.Integration.Education.ApplyEducationParticipation;
using Matrix.Population.Infrastructure.Integration.Education;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Infrastructure.Consumers.Education
{
    internal static class EducationStudentParticipationCommandMapper
    {
        internal static ApplyEducationParticipationCommand Map(
            EducationStudentParticipationBatchV1 message,
            Guid messageId,
            string consumerName)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Students);
            if (message.Students.Count is < 1 or > 1000 || message.Students.Any(student => student is null))
                throw new ArgumentException("Invalid education participation batch.", nameof(message));
            var routines = new Dictionary<EducationDailyRoutineV1, PersonRoutineProfile>();

            PersonRoutineProfile? MapRoutine(EducationDailyRoutineV1? routine)
            {
                if (routine is null) return null;
                if (!routines.TryGetValue(routine, out var profile))
                {
                    profile = EducationRoutineMapper.FromContract(routine);
                    routines.Add(routine, profile);
                }
                return profile;
            }

            StudentEducationParticipationInput[] students = message.Students
               .Select(student => new StudentEducationParticipationInput(
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
                    Economics: student.EconomicEffects is null ? null : EducationEconomicEffectsMapper.FromContract(student.EconomicEffects),
                    Routine: MapRoutine(student.DailyRoutine)))
               .ToArray();

            return new ApplyEducationParticipationCommand(
                SimulationHostId: message.SimulationHostId,
                IntegrationMessageId: messageId,
                ConsumerName: consumerName,
                SnapshotDate: message.SnapshotDate,
                OccurredAtUtc: message.OccurredAtUtc,
                BatchNumber: message.BatchNumber,
                TotalBatches: message.TotalBatches,
                Students: students);
        }
    }
}
