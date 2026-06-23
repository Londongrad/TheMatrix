using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Population.Contracts.Events;

namespace Matrix.Education.Integration.Consumers
{
    internal static class PopulationResidentFactsCommandMapper
    {
        internal static SynchronizeStudentProfilesCommand Map(PopulationResidentFactsBatchV1 message)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Residents);

            if (string.IsNullOrWhiteSpace(message.CorrelationId))
                throw new ArgumentException(
                    message: "A resident fact batch correlation identifier is required.",
                    paramName: nameof(message));

            if (message.TotalBatches <= 0 ||
                message.BatchNumber <= 0 ||
                message.BatchNumber > message.TotalBatches)
                throw new ArgumentException(
                    message: "Resident fact batch position metadata is invalid.",
                    paramName: nameof(message));

            SynchronizeStudentProfileItem[] profiles = message.Residents
               .Select(resident => new SynchronizeStudentProfileItem(
                    ResidentId: resident.ResidentId,
                    BirthDate: resident.BirthDate,
                    IsAlive: resident.IsAlive,
                    IsActive: resident.IsActive,
                    SourceRevision: message.SourceRevision))
               .ToArray();

            return new SynchronizeStudentProfilesCommand(
                SimulationHostId: message.SimulationHostId,
                SynchronizedAtUtc: message.SynchronizedAtUtc,
                Profiles: profiles);
        }
    }
}
