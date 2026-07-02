using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Facilities;

public static class ClassicCityCareFacilityProvisioningFactory
{
    public const long InitialSourceRevision = 0;

    public static CareFacilityProvisioningBatch Create(
        City city,
        IReadOnlyCollection<CityAnchor> anchors)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(anchors);

        CareFacilityProvisioning[] facilities = anchors
           .Where(anchor => anchor.Type == CityAnchorType.Hospital)
           .OrderBy(anchor => anchor.Id.Value)
           .Select(anchor => new CareFacilityProvisioning(
                FacilityId: anchor.Id.Value,
                Name: anchor.Name.Value,
                Kind: anchor.Type.ToString(),
                LocationAnchorId: anchor.Id.Value,
                DailyPatientCapacity: anchor.Capacity,
                IsActive: true))
           .ToArray();

        return new CareFacilityProvisioningBatch(
            SimulationHostId: city.Id.Value,
            SourceRevision: InitialSourceRevision,
            SynchronizedAtUtc: city.CreatedAtUtc,
            CorrelationId: $"simulation:{city.Id.Value:N}:care-facilities:{InitialSourceRevision}",
            Facilities: facilities);
    }
}
