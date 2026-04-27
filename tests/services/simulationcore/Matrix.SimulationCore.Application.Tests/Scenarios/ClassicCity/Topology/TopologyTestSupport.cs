using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;

internal static class TopologyTestSupport
{
    internal static readonly DateTimeOffset CreatedAtUtc = new(2048, 3, 4, 5, 6, 7, TimeSpan.Zero);

    internal static District CreateDistrict(
        CityId? cityId = null,
        string name = "Central District")
    {
        return District.Create(
            cityId ?? new CityId(Guid.NewGuid()),
            new DistrictName(name),
            anchorX: 10.5m,
            anchorY: 20.5m,
            createdAtUtc: CreatedAtUtc);
    }

    internal static CityAnchor CreateCityAnchor(
        CityId? cityId = null,
        DistrictId? districtId = null,
        string name = "General Hospital")
    {
        return CityAnchor.Create(
            cityId ?? new CityId(Guid.NewGuid()),
            districtId ?? new DistrictId(Guid.NewGuid()),
            RoadNodeId.New(),
            new CityAnchorName(name),
            CityAnchorType.Hospital,
            capacity: 500,
            positionX: 15.25m,
            positionY: 30.75m,
            createdAtUtc: CreatedAtUtc);
    }

    internal static ResidentialBuilding CreateResidentialBuilding(
        CityId? cityId = null,
        DistrictId? districtId = null,
        string name = "North Tower")
    {
        return ResidentialBuilding.Create(
            cityId ?? new CityId(Guid.NewGuid()),
            districtId ?? new DistrictId(Guid.NewGuid()),
            RoadNodeId.New(),
            new ResidentialBuildingName(name),
            ResidentialBuildingType.Tower,
            ResidentCapacity.From(240),
            positionX: 18.5m,
            positionY: 32.5m,
            createdAtUtc: CreatedAtUtc);
    }

    internal static RoadNode CreateRoadNode(
        CityId? cityId = null,
        DistrictId? districtId = null,
        string name = "Central Junction")
    {
        return RoadNode.Create(
            cityId ?? new CityId(Guid.NewGuid()),
            districtId ?? new DistrictId(Guid.NewGuid()),
            name,
            RoadNodeType.Junction,
            positionX: 22.5m,
            positionY: 44.5m,
            createdAtUtc: CreatedAtUtc);
    }

    internal static RoadSegment CreateRoadSegment(
        CityId? cityId = null,
        DistrictId? districtId = null,
        RoadNodeId? fromRoadNodeId = null,
        RoadNodeId? toRoadNodeId = null,
        string name = "Main Connector")
    {
        return RoadSegment.Create(
            cityId ?? new CityId(Guid.NewGuid()),
            districtId ?? new DistrictId(Guid.NewGuid()),
            fromRoadNodeId ?? RoadNodeId.New(),
            toRoadNodeId ?? RoadNodeId.New(),
            name,
            RoadSegmentType.Collector,
            lengthMeters: 180m,
            createdAtUtc: CreatedAtUtc);
    }

    internal sealed class FakeCityAnchorRepository : ICityAnchorRepository
    {
        public IReadOnlyList<CityAnchor> Anchors { get; set; } = Array.Empty<CityAnchor>();
        public CityId? RequestedCityId { get; private set; }

        public Task<CityAnchor?> GetByIdAsync(CityAnchorId anchorId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddRangeAsync(IReadOnlyCollection<CityAnchor> anchors, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<CityAnchor>> ListByCityIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Anchors);
        }
    }

    internal sealed class FakeDistrictRepository : IDistrictRepository
    {
        public IReadOnlyList<District> Districts { get; set; } = Array.Empty<District>();
        public CityId? RequestedCityId { get; private set; }

        public Task AddRangeAsync(IReadOnlyCollection<District> districts, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<District>> ListByCityIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Districts);
        }
    }

    internal sealed class FakeResidentialBuildingRepository : IResidentialBuildingRepository
    {
        public IReadOnlyList<ResidentialBuilding> Buildings { get; set; } = Array.Empty<ResidentialBuilding>();
        public CityId? RequestedCityId { get; private set; }
        public DistrictId? RequestedDistrictId { get; private set; }

        public Task<ResidentialBuilding?> GetByIdAsync(ResidentialBuildingId buildingId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddRangeAsync(IReadOnlyCollection<ResidentialBuilding> buildings, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ResidentialBuilding>> ListByCityIdAsync(
            CityId cityId,
            DistrictId? districtId,
            CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            RequestedDistrictId = districtId;
            return Task.FromResult(Buildings);
        }
    }

    internal sealed class FakeRoadNodeRepository : IRoadNodeRepository
    {
        public IReadOnlyList<RoadNode> RoadNodes { get; set; } = Array.Empty<RoadNode>();
        public CityId? RequestedCityId { get; private set; }

        public Task AddRangeAsync(IReadOnlyCollection<RoadNode> roadNodes, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<RoadNode>> ListByCityIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            return Task.FromResult(RoadNodes);
        }
    }

    internal sealed class FakeRoadSegmentRepository : IRoadSegmentRepository
    {
        public IReadOnlyList<RoadSegment> RoadSegments { get; set; } = Array.Empty<RoadSegment>();
        public CityId? RequestedCityId { get; private set; }

        public Task AddRangeAsync(IReadOnlyCollection<RoadSegment> roadSegments, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<RoadSegment>> ListByCityIdAsync(CityId cityId, CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            return Task.FromResult(RoadSegments);
        }
    }
}
