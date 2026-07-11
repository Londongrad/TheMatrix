using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Institutions;

public static class ClassicCityEducationInstitutionProvisioningFactory
{
    public const long InitialSourceRevision = 0;

    public static EducationInstitutionProvisioningBatch Create(
        City city,
        IReadOnlyCollection<CityAnchor> anchors)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(anchors);

        EducationInstitutionProvisioning[] institutions = anchors
           .Where(anchor => anchor.Type == CityAnchorType.School)
           .OrderBy(anchor => anchor.Id.Value)
           .Select(anchor => new EducationInstitutionProvisioning(
                InstitutionId: anchor.Id.Value,
                Name: anchor.Name.Value,
                Kind: anchor.Type.ToString(),
                LocationAnchorId: anchor.Id.Value,
                Capacity: anchor.Capacity,
                IsActive: true))
           .ToArray();

        return new EducationInstitutionProvisioningBatch(
            SimulationHostId: city.Id.Value,
            SourceRevision: InitialSourceRevision,
            SynchronizedAtUtc: city.CreatedAtUtc,
            CorrelationId: $"simulation:{city.Id.Value:N}:education-institutions:{InitialSourceRevision}",
            Institutions: institutions);
    }
}
