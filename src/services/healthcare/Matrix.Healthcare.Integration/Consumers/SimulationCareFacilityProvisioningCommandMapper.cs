using Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities;
using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.Healthcare.Integration.Consumers;

internal static class SimulationCareFacilityProvisioningCommandMapper
{
    internal static SynchronizeCareFacilitiesCommand Map(
        SimulationCareFacilityProvisioningBatchV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Facilities);

        if (string.IsNullOrWhiteSpace(message.CorrelationId))
            throw new ArgumentException(
                message: "A care facility provisioning correlation identifier is required.",
                paramName: nameof(message));

        if (message.TotalBatches <= 0 ||
            message.BatchNumber <= 0 ||
            message.BatchNumber > message.TotalBatches)
            throw new ArgumentException(
                message: "Care facility provisioning batch position metadata is invalid.",
                paramName: nameof(message));

        SynchronizeCareFacilityItem[] facilities = message.Facilities
           .Select(facility => new SynchronizeCareFacilityItem(
                FacilityId: facility.FacilityId,
                Name: facility.Name,
                Kind: facility.Kind,
                LocationAnchorId: facility.LocationAnchorId,
                DailyPatientCapacity: facility.DailyPatientCapacity,
                IsActive: facility.IsActive))
           .ToArray();

        return new SynchronizeCareFacilitiesCommand(
            SimulationHostId: message.SimulationHostId,
            SourceRevision: message.SourceRevision,
            SynchronizedAtUtc: message.SynchronizedAtUtc,
            Facilities: facilities);
    }
}
