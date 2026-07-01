using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Population.Contracts.Events;

namespace Matrix.Healthcare.Integration.Consumers
{
    internal static class PopulationResidentFactsCommandMapper
    {
        internal static SynchronizePatientProfilesCommand Map(PopulationResidentFactsBatchV1 message)
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

            SynchronizePatientProfileItem[] profiles = message.Residents
               .Select(resident => new SynchronizePatientProfileItem(
                    PatientId: resident.ResidentId,
                    BirthDate: resident.BirthDate,
                    Sex: MapSex(resident.Sex),
                    IsAlive: resident.IsAlive,
                    IsActive: resident.IsActive,
                    SourceRevision: message.SourceRevision,
                    LifecycleRevision: resident.LifecycleRevision))
               .ToArray();

            return new SynchronizePatientProfilesCommand(
                SimulationHostId: message.SimulationHostId,
                SynchronizedAtUtc: message.SynchronizedAtUtc,
                Profiles: profiles);
        }

        private static PatientSex MapSex(string? value)
        {
            if (string.Equals(value, "Male", StringComparison.OrdinalIgnoreCase))
                return PatientSex.Male;

            if (string.Equals(value, "Female", StringComparison.OrdinalIgnoreCase))
                return PatientSex.Female;

            throw new ArgumentException(
                message: $"Population resident sex '{value}' is not supported by Healthcare.",
                paramName: nameof(value));
        }
    }
}
