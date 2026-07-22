using Matrix.Education.Contracts.Events;
using Matrix.Population.Application.Integration.Education.ApplyEducationParticipation;
using Matrix.Population.Infrastructure.Integration.Education;

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
                    Economics: student.EconomicEffects is null ? null : EducationEconomicEffectsMapper.FromContract(student.EconomicEffects)))
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
