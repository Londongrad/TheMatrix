using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.Education.Integration.Consumers;

internal static class SimulationEducationInstitutionProvisioningCommandMapper
{
    internal static SynchronizeEducationInstitutionsCommand Map(
        SimulationEducationInstitutionProvisioningBatchV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Institutions);

        if (string.IsNullOrWhiteSpace(message.CorrelationId))
            throw new ArgumentException(
                message: "An education institution provisioning correlation identifier is required.",
                paramName: nameof(message));

        if (message.TotalBatches <= 0 ||
            message.BatchNumber <= 0 ||
            message.BatchNumber > message.TotalBatches)
            throw new ArgumentException(
                message: "Education institution provisioning batch position metadata is invalid.",
                paramName: nameof(message));

        SynchronizeEducationInstitutionItem[] institutions = message.Institutions
           .Select(institution => new SynchronizeEducationInstitutionItem(
                InstitutionId: institution.InstitutionId,
                Name: institution.Name,
                Kind: institution.Kind,
                Capacity: institution.Capacity,
                IsActive: institution.IsActive,
                LocationAnchorId: institution.LocationAnchorId))
           .ToArray();

        return new SynchronizeEducationInstitutionsCommand(
            SimulationHostId: message.SimulationHostId,
            SourceRevision: message.SourceRevision,
            SynchronizedAtUtc: message.SynchronizedAtUtc,
            Institutions: institutions);
    }
}
