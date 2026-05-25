using System.Security.Cryptography;
using System.Text;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology.Abstractions;
using Matrix.SimulationCore.Application.Services.Generation.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology
{
    /// <summary>
    ///     Creates a deterministic starter topology for a newly created city.
    /// </summary>
    public sealed class CityTopologyBootstrapFactory(ICityGenerationContentCatalog generationContentCatalog)
        : ICityTopologyBootstrapFactory
    {
        public CityTopologySeed CreateInitial(City city)
        {
            ArgumentNullException.ThrowIfNull(city);

            var random = new DeterministicRandom(BuildSeed(city));
            GenerationPlan plan = BuildGenerationPlan(
                profile: city.GenerationProfile,
                random: random);
            DateTimeOffset createdAtUtc = city.CreatedAtUtc;

            List<District> districts = CreateDistricts(
                city: city,
                createdAtUtc: createdAtUtc,
                random: random,
                plan: plan);

            List<ResidentialBuildingBlueprint> buildingBlueprints = CreateResidentialBuildingBlueprints(
                city: city,
                districts: districts,
                createdAtUtc: createdAtUtc,
                random: random,
                plan: plan);
            List<CityAnchorBlueprint> anchorBlueprints = CreateCityAnchorBlueprints(
                city: city,
                districts: districts,
                random: random);
            RoadGraphLayout roadGraph = CreateRoadGraph(
                city: city,
                districts: districts,
                buildingBlueprints: buildingBlueprints,
                anchorBlueprints: anchorBlueprints,
                createdAtUtc: createdAtUtc,
                random: random);

            return new CityTopologySeed(
                Districts: districts,
                ResidentialBuildings: roadGraph.ResidentialBuildings,
                Anchors: roadGraph.Anchors,
                RoadNodes: roadGraph.RoadNodes,
                RoadSegments: roadGraph.RoadSegments);
        }

        private GenerationPlan BuildGenerationPlan(
            CityGenerationProfile profile,
            DeterministicRandom random)
        {
            int targetPopulation = ResolveTargetPopulation(profile);
            int topologyPopulationBasis = Math.Max(
                val1: targetPopulation,
                val2: GetTopologyPopulationFloor(profile.SizeTier));
            int targetResidentialCapacity = ResolveResidentialCapacityTarget(
                profile: profile,
                topologyPopulationBasis: topologyPopulationBasis,
                random: random);
            int districtCount = ResolveDistrictCount(
                profile: profile,
                targetResidentialCapacity: targetResidentialCapacity);

            return new GenerationPlan(
                TargetPopulation: targetPopulation,
                TopologyPopulationBasis: topologyPopulationBasis,
                TargetResidentialCapacity: targetResidentialCapacity,
                DistrictCount: districtCount);
        }

        private List<District> CreateDistricts(
            City city,
            DateTimeOffset createdAtUtc,
            DeterministicRandom random,
            GenerationPlan plan)
        {
            var availableNames = generationContentCatalog.DistrictNamePresets
               .Where(x => !string.Equals(
                    a: x,
                    b: "Central District",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
               .ToList();
            Shuffle(
                items: availableNames,
                random: random);

            var districts = new List<District>
            {
                District.Create(
                    cityId: city.Id,
                    name: new DistrictName("Central District"),
                    anchorX: 50m,
                    anchorY: 50m,
                    createdAtUtc: createdAtUtc)
            };

            for (int i = 1; i < plan.DistrictCount; i++)
            {
                string districtName = i - 1 < availableNames.Count
                    ? availableNames[i - 1]
                    : $"Sector {i}";

                districts.Add(
                    District.Create(
                        cityId: city.Id,
                        name: new DistrictName(districtName),
                        anchorX: ResolveDistrictAnchorX(
                            districtIndex: i,
                            districtCount: plan.DistrictCount,
                            random: random),
                        anchorY: ResolveDistrictAnchorY(
                            districtIndex: i,
                            districtCount: plan.DistrictCount,
                            random: random),
                        createdAtUtc: createdAtUtc));
            }

            return districts;
        }

        private static List<ResidentialBuildingBlueprint> CreateResidentialBuildingBlueprints(
            City city,
            IReadOnlyList<District> districts,
            DateTimeOffset createdAtUtc,
            DeterministicRandom random,
            GenerationPlan plan)
        {
            var buildings = new List<ResidentialBuildingBlueprint>();
            DistrictArchetype[] districtArchetypes = BuildDistrictArchetypes(
                profile: city.GenerationProfile,
                districtCount: districts.Count,
                random: random);
            int[] districtCapacityTargets = BuildDistrictCapacityTargets(
                profile: city.GenerationProfile,
                totalCapacityTarget: plan.TargetResidentialCapacity,
                districtCount: districts.Count,
                districtArchetypes: districtArchetypes,
                random: random);

            for (int districtIndex = 0; districtIndex < districts.Count; districtIndex++)
            {
                District district = districts[districtIndex];
                bool isCentral = districtIndex == 0;
                DistrictArchetype districtArchetype = districtArchetypes[districtIndex];
                int districtCapacityTarget = districtCapacityTargets[districtIndex];
                int districtCapacityBuilt = 0;

                string districtLabel = district.Name.Value.Replace(
                    oldValue: " District",
                    newValue: string.Empty,
                    comparisonType: StringComparison.Ordinal);
                var typeCounters = new Dictionary<ResidentialBuildingType, int>();
                int placementIndex = 0;

                while (districtCapacityBuilt < districtCapacityTarget || districtCapacityBuilt == 0)
                {
                    ResidentialBuildingType type = GetBuildingType(
                        profile: city.GenerationProfile,
                        isCentral: isCentral,
                        archetype: districtArchetype,
                        random: random);

                    int sequence = typeCounters.TryGetValue(
                        key: type,
                        value: out int current)
                        ? current + 1
                        : 1;
                    typeCounters[type] = sequence;

                    int residentCapacity = GetResidentCapacity(
                        type: type,
                        profile: city.GenerationProfile,
                        topologyPopulationBasis: plan.TopologyPopulationBasis,
                        isCentral: isCentral,
                        archetype: districtArchetype,
                        random: random);

                    (decimal positionX, decimal positionY) = ResolveBuildingPosition(
                        district: district,
                        placementIndex: placementIndex,
                        random: random);

                    buildings.Add(
                        new ResidentialBuildingBlueprint(
                            District: district,
                            Name: CreateBuildingName(
                                districtLabel: districtLabel,
                                type: type,
                                sequence: sequence),
                            Type: type,
                            ResidentCapacity: residentCapacity,
                            PositionX: positionX,
                            PositionY: positionY,
                            CreatedAtUtc: createdAtUtc));

                    districtCapacityBuilt += residentCapacity;
                    placementIndex++;
                }
            }

            return buildings;
        }

        private static List<CityAnchorBlueprint> CreateCityAnchorBlueprints(
            City city,
            IReadOnlyList<District> districts,
            DeterministicRandom random)
        {
            var anchors = new List<CityAnchorBlueprint>();

            for (int districtIndex = 0; districtIndex < districts.Count; districtIndex++)
            {
                District district = districts[districtIndex];

                anchors.Add(
                    CreateSchoolBlueprint(
                        district: district,
                        districtIndex: districtIndex,
                        random: random));

                int workplaceCount = ResolveWorkplaceAnchorCount(
                    city: city,
                    districtIndex: districtIndex);

                for (int workplaceIndex = 0; workplaceIndex < workplaceCount; workplaceIndex++)
                    anchors.Add(
                        CreateWorkplaceBlueprint(
                            city: city,
                            district: district,
                            districtIndex: districtIndex,
                            workplaceIndex: workplaceIndex,
                            random: random));
            }

            foreach ((District district, int hospitalIndex) in ResolveHospitalPlacements(
                         city: city,
                         districts: districts))
                anchors.Add(
                    CreateHospitalBlueprint(
                        city: city,
                        district: district,
                        hospitalIndex: hospitalIndex,
                        random: random));

            return anchors;
        }

        private RoadGraphLayout CreateRoadGraph(
            City city,
            IReadOnlyList<District> districts,
            IReadOnlyList<ResidentialBuildingBlueprint> buildingBlueprints,
            IReadOnlyList<CityAnchorBlueprint> anchorBlueprints,
            DateTimeOffset createdAtUtc,
            DeterministicRandom random)
        {
            var streetNames = generationContentCatalog.StreetNamePresets.ToList();
            Shuffle(
                items: streetNames,
                random: random);

            var roadNodes = new List<RoadNode>();
            var roadSegments = new List<RoadSegment>();
            var residentialBuildings = new List<ResidentialBuilding>();
            var anchors = new List<CityAnchor>();
            var districtHubByDistrictId = new Dictionary<DistrictId, RoadNode>(districts.Count);

            for (int districtIndex = 0; districtIndex < districts.Count; districtIndex++)
            {
                District district = districts[districtIndex];
                string hubName = districtIndex == 0
                    ? "Central Hub"
                    : $"{district.Name.Value} Hub";

                var districtHub = RoadNode.Create(
                    cityId: city.Id,
                    districtId: district.Id,
                    name: hubName,
                    type: RoadNodeType.DistrictHub,
                    positionX: district.AnchorX,
                    positionY: district.AnchorY,
                    createdAtUtc: createdAtUtc);

                districtHubByDistrictId[district.Id] = districtHub;
                roadNodes.Add(districtHub);
            }

            District centralDistrict = districts[0];
            RoadNode centralHub = districtHubByDistrictId[centralDistrict.Id];
            int namedStreetIndex = 0;

            foreach (District district in districts.Skip(1))
            {
                RoadNode districtHub = districtHubByDistrictId[district.Id];
                string arterialName = namedStreetIndex < streetNames.Count
                    ? streetNames[namedStreetIndex++]
                    : $"{district.Name.Value} Connector";

                roadSegments.Add(
                    RoadSegment.Create(
                        cityId: city.Id,
                        districtId: district.Id,
                        fromRoadNodeId: centralHub.Id,
                        toRoadNodeId: districtHub.Id,
                        name: arterialName,
                        type: RoadSegmentType.Arterial,
                        lengthMeters: EstimateLengthMeters(
                            fromX: centralHub.PositionX,
                            fromY: centralHub.PositionY,
                            toX: districtHub.PositionX,
                            toY: districtHub.PositionY),
                        createdAtUtc: createdAtUtc));
            }

            foreach (IGrouping<DistrictId, ResidentialBuildingBlueprint> districtBuildings in
                     buildingBlueprints.GroupBy(x => x.District.Id))
            {
                RoadNode districtHub = districtHubByDistrictId[districtBuildings.Key];
                int localSequence = 1;

                foreach (ResidentialBuildingBlueprint blueprint in districtBuildings)
                {
                    var accessNode = RoadNode.Create(
                        cityId: city.Id,
                        districtId: blueprint.District.Id,
                        name: $"{blueprint.Name} Access",
                        type: RoadNodeType.ResidentialAccess,
                        positionX: blueprint.PositionX,
                        positionY: blueprint.PositionY,
                        createdAtUtc: createdAtUtc);

                    roadNodes.Add(accessNode);
                    roadSegments.Add(
                        RoadSegment.Create(
                            cityId: city.Id,
                            districtId: blueprint.District.Id,
                            fromRoadNodeId: districtHub.Id,
                            toRoadNodeId: accessNode.Id,
                            name: $"{blueprint.District.Name.Value} Local {localSequence}",
                            type: districtHub.Id == centralHub.Id
                                ? RoadSegmentType.Collector
                                : RoadSegmentType.LocalAccess,
                            lengthMeters: EstimateLengthMeters(
                                fromX: districtHub.PositionX,
                                fromY: districtHub.PositionY,
                                toX: accessNode.PositionX,
                                toY: accessNode.PositionY),
                            createdAtUtc: createdAtUtc));

                    residentialBuildings.Add(
                        ResidentialBuilding.Create(
                            cityId: city.Id,
                            districtId: blueprint.District.Id,
                            accessRoadNodeId: accessNode.Id,
                            name: new ResidentialBuildingName(blueprint.Name),
                            type: blueprint.Type,
                            residentCapacity: ResidentCapacity.From(blueprint.ResidentCapacity),
                            positionX: blueprint.PositionX,
                            positionY: blueprint.PositionY,
                            createdAtUtc: blueprint.CreatedAtUtc));

                    localSequence++;
                }
            }

            foreach (IGrouping<DistrictId, CityAnchorBlueprint> districtAnchors in anchorBlueprints.GroupBy(x
                         => x.District.Id))
            {
                RoadNode districtHub = districtHubByDistrictId[districtAnchors.Key];
                int localSequence = 1;

                foreach (CityAnchorBlueprint blueprint in districtAnchors)
                {
                    var accessNode = RoadNode.Create(
                        cityId: city.Id,
                        districtId: blueprint.District.Id,
                        name: $"{blueprint.Name} Access",
                        type: RoadNodeType.AnchorAccess,
                        positionX: blueprint.PositionX,
                        positionY: blueprint.PositionY,
                        createdAtUtc: createdAtUtc);

                    roadNodes.Add(accessNode);
                    roadSegments.Add(
                        RoadSegment.Create(
                            cityId: city.Id,
                            districtId: blueprint.District.Id,
                            fromRoadNodeId: districtHub.Id,
                            toRoadNodeId: accessNode.Id,
                            name: $"{blueprint.District.Name.Value} Civic {localSequence}",
                            type: blueprint.Type == CityAnchorType.Workplace
                                ? RoadSegmentType.Collector
                                : RoadSegmentType.LocalAccess,
                            lengthMeters: EstimateLengthMeters(
                                fromX: districtHub.PositionX,
                                fromY: districtHub.PositionY,
                                toX: accessNode.PositionX,
                                toY: accessNode.PositionY),
                            createdAtUtc: createdAtUtc));

                    anchors.Add(
                        CityAnchor.Create(
                            cityId: city.Id,
                            districtId: blueprint.District.Id,
                            accessRoadNodeId: accessNode.Id,
                            name: new CityAnchorName(blueprint.Name),
                            type: blueprint.Type,
                            capacity: blueprint.Capacity,
                            positionX: blueprint.PositionX,
                            positionY: blueprint.PositionY,
                            createdAtUtc: blueprint.CreatedAtUtc));

                    localSequence++;
                }
            }

            return new RoadGraphLayout(
                ResidentialBuildings: residentialBuildings,
                Anchors: anchors,
                RoadNodes: roadNodes,
                RoadSegments: roadSegments);
        }

        private static CityAnchorBlueprint CreateSchoolBlueprint(
            District district,
            int districtIndex,
            DeterministicRandom random)
        {
            (decimal x, decimal y) = ResolveAnchorPosition(
                district: district,
                slotIndex: districtIndex + 1,
                radialBand: 0,
                random: random);

            int capacity = 280 +
                           (districtIndex == 0
                               ? 120
                               : 0) +
                           random.NextInt(
                               minInclusive: 0,
                               maxExclusive: 140);

            return new CityAnchorBlueprint(
                District: district,
                Name: districtIndex == 0
                    ? "Central Education Complex"
                    : $"{district.Name.Value} School",
                Type: CityAnchorType.School,
                Capacity: capacity,
                PositionX: x,
                PositionY: y,
                CreatedAtUtc: district.CreatedAtUtc);
        }

        private static CityAnchorBlueprint CreateWorkplaceBlueprint(
            City city,
            District district,
            int districtIndex,
            int workplaceIndex,
            DeterministicRandom random)
        {
            (decimal x, decimal y) = ResolveAnchorPosition(
                district: district,
                slotIndex: districtIndex + workplaceIndex + 2,
                radialBand: workplaceIndex + 1,
                random: random);

            int baseCapacity = city.GenerationProfile.SizeTier switch
            {
                CitySizeTier.Small => 90,
                CitySizeTier.Large => 240,
                _ => 150
            };
            baseCapacity += city.GenerationProfile.DevelopmentLevel switch
            {
                CityDevelopmentLevel.Struggling => -20,
                CityDevelopmentLevel.Advanced => 35,
                _ => 0
            };
            baseCapacity += districtIndex == 0
                ? 55
                : 0;
            int capacity = baseCapacity +
                           random.NextInt(
                               minInclusive: 0,
                               maxExclusive: 90);

            string workplaceName = workplaceIndex switch
            {
                0 => $"{district.Name.Value} Commerce Hub",
                1 => $"{district.Name.Value} Industry Yard",
                _ => $"{district.Name.Value} Work Center {workplaceIndex + 1}"
            };

            return new CityAnchorBlueprint(
                District: district,
                Name: workplaceName,
                Type: CityAnchorType.Workplace,
                Capacity: capacity,
                PositionX: x,
                PositionY: y,
                CreatedAtUtc: district.CreatedAtUtc);
        }

        private static CityAnchorBlueprint CreateHospitalBlueprint(
            City city,
            District district,
            int hospitalIndex,
            DeterministicRandom random)
        {
            (decimal x, decimal y) = ResolveAnchorPosition(
                district: district,
                slotIndex: hospitalIndex + 5,
                radialBand: 2,
                random: random);

            int baseCapacity = city.GenerationProfile.SizeTier switch
            {
                CitySizeTier.Small => 70,
                CitySizeTier.Large => 210,
                _ => 130
            };
            baseCapacity += city.GenerationProfile.DevelopmentLevel switch
            {
                CityDevelopmentLevel.Struggling => -15,
                CityDevelopmentLevel.Advanced => 30,
                _ => 0
            };

            return new CityAnchorBlueprint(
                District: district,
                Name: hospitalIndex == 0
                    ? "Central General Hospital"
                    : $"{district.Name.Value} Clinic",
                Type: CityAnchorType.Hospital,
                Capacity: baseCapacity +
                          random.NextInt(
                              minInclusive: 0,
                              maxExclusive: 50),
                PositionX: x,
                PositionY: y,
                CreatedAtUtc: district.CreatedAtUtc);
        }

        private static int ResolveWorkplaceAnchorCount(
            City city,
            int districtIndex)
        {
            int baseCount = city.GenerationProfile.SizeTier switch
            {
                CitySizeTier.Small => districtIndex == 0
                    ? 2
                    : 1,
                CitySizeTier.Large => districtIndex == 0
                    ? 5
                    : 3,
                _ => districtIndex == 0
                    ? 3
                    : 2
            };

            if (city.GenerationProfile.EconomyProfile == CityEconomyProfile.Affluent && districtIndex > 0)
                baseCount++;

            if (city.GenerationProfile.DevelopmentLevel == CityDevelopmentLevel.Advanced && districtIndex == 0)
                baseCount++;

            return baseCount;
        }

        private static IReadOnlyList<(District District, int HospitalIndex)> ResolveHospitalPlacements(
            City city,
            IReadOnlyList<District> districts)
        {
            int hospitalCount = city.GenerationProfile.SizeTier switch
            {
                CitySizeTier.Small => 1,
                CitySizeTier.Large => 3,
                _ => 2
            };

            hospitalCount += city.GenerationProfile.DevelopmentLevel == CityDevelopmentLevel.Advanced
                ? 1
                : 0;

            hospitalCount = Math.Min(
                val1: hospitalCount,
                val2: districts.Count);

            var placements = new List<(District District, int HospitalIndex)>(hospitalCount);

            for (int i = 0; i < hospitalCount; i++)
            {
                District district = i == 0
                    ? districts[0]
                    : districts[Math.Min(
                        val1: i,
                        val2: districts.Count - 1)];
                placements.Add((district, i));
            }

            return placements;
        }

        private static (decimal X, decimal Y) ResolveAnchorPosition(
            District district,
            int slotIndex,
            int radialBand,
            DeterministicRandom random)
        {
            decimal radius = 6.5m +
                             (radialBand * 2.2m) +
                             random.NextDecimal(
                                 minInclusive: -0.55m,
                                 maxInclusive: 0.55m);
            double angle = (slotIndex * (Math.PI / 3.5d)) +
                           (double)random.NextDecimal(
                               minInclusive: -0.14m,
                               maxInclusive: 0.14m);
            decimal x = district.AnchorX + ((decimal)Math.Cos(angle) * radius);
            decimal y = district.AnchorY + ((decimal)Math.Sin(angle) * radius);

            return (
                ClampCoordinate(x),
                ClampCoordinate(y));
        }

        private static decimal ResolveDistrictAnchorX(
            int districtIndex,
            int districtCount,
            DeterministicRandom random)
        {
            if (districtIndex == 0 || districtCount <= 1)
                return 50m;

            (decimal x, _) = ResolveOuterDistrictAnchor(
                districtIndex: districtIndex,
                districtCount: districtCount,
                random: random);

            return x;
        }

        private static decimal ResolveDistrictAnchorY(
            int districtIndex,
            int districtCount,
            DeterministicRandom random)
        {
            if (districtIndex == 0 || districtCount <= 1)
                return 50m;

            (_, decimal y) = ResolveOuterDistrictAnchor(
                districtIndex: districtIndex,
                districtCount: districtCount,
                random: random);

            return y;
        }

        private static (decimal X, decimal Y) ResolveOuterDistrictAnchor(
            int districtIndex,
            int districtCount,
            DeterministicRandom random)
        {
            int outerCount = Math.Max(
                val1: 1,
                val2: districtCount - 1);
            double baseAngle = (-Math.PI / 2d) +
                               ((districtIndex - 1) * (2d * Math.PI / outerCount));
            double angle = baseAngle +
                           (double)random.NextDecimal(
                               minInclusive: -0.11m,
                               maxInclusive: 0.11m);
            decimal radius = 28m +
                             random.NextDecimal(
                                 minInclusive: -2.75m,
                                 maxInclusive: 2.75m);
            decimal x = 50m + ((decimal)Math.Cos(angle) * radius);
            decimal y = 50m + ((decimal)Math.Sin(angle) * radius);

            return (
                ClampCoordinate(x),
                ClampCoordinate(y));
        }

        private static (decimal X, decimal Y) ResolveBuildingPosition(
            District district,
            int placementIndex,
            DeterministicRandom random)
        {
            const int maxPerRing = 8;

            int ringIndex = placementIndex / maxPerRing;
            int slotIndex = placementIndex % maxPerRing;
            decimal ringRadius = 4.5m +
                                 (ringIndex * 2.5m) +
                                 random.NextDecimal(
                                     minInclusive: -0.45m,
                                     maxInclusive: 0.45m);
            double baseAngle = slotIndex * (2d * Math.PI / maxPerRing);
            double angle = baseAngle +
                           (double)random.NextDecimal(
                               minInclusive: -0.18m,
                               maxInclusive: 0.18m);
            decimal x = district.AnchorX + ((decimal)Math.Cos(angle) * ringRadius);
            decimal y = district.AnchorY + ((decimal)Math.Sin(angle) * ringRadius);

            return (
                ClampCoordinate(x),
                ClampCoordinate(y));
        }

        private static decimal EstimateLengthMeters(
            decimal fromX,
            decimal fromY,
            decimal toX,
            decimal toY)
        {
            decimal dx = toX - fromX;
            decimal dy = toY - fromY;
            double euclideanDistance = Math.Sqrt((double)((dx * dx) + (dy * dy)));
            decimal lengthMeters = (decimal)euclideanDistance * 90m;

            return Math.Max(
                val1: 30m,
                val2: decimal.Round(
                    d: lengthMeters,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero));
        }

        private static decimal ClampCoordinate(decimal value)
        {
            decimal clamped = Math.Clamp(
                value: value,
                min: 4m,
                max: 96m);

            return decimal.Round(
                d: clamped,
                decimals: TopologyMapRules.CoordinateScale,
                mode: MidpointRounding.AwayFromZero);
        }

        private static int ResolveTargetPopulation(CityGenerationProfile profile)
        {
            return profile.PlannedPeopleCount ?? GetFallbackPopulationTarget(profile);
        }

        private static int GetFallbackPopulationTarget(CityGenerationProfile profile)
        {
            int basePopulation = profile.SizeTier switch
            {
                CitySizeTier.Small => 1_000,
                CitySizeTier.Medium => 10_000,
                CitySizeTier.Large => 35_000,
                _ => 10_000
            };

            decimal densityFactor = profile.UrbanDensity switch
            {
                UrbanDensity.Sparse => 0.85m,
                UrbanDensity.Dense => 1.25m,
                _ => 1.0m
            };

            decimal developmentFactor = profile.DevelopmentLevel switch
            {
                CityDevelopmentLevel.Struggling => 0.90m,
                CityDevelopmentLevel.Advanced => 1.10m,
                _ => 1.0m
            };

            return Math.Max(
                val1: 100,
                val2: (int)Math.Round(
                    d: basePopulation * densityFactor * developmentFactor,
                    mode: MidpointRounding.AwayFromZero));
        }

        private static int GetTopologyPopulationFloor(CitySizeTier sizeTier)
        {
            return sizeTier switch
            {
                CitySizeTier.Small => 350,
                CitySizeTier.Medium => 800,
                CitySizeTier.Large => 1_600,
                _ => 800
            };
        }

        private static int ResolveResidentialCapacityTarget(
            CityGenerationProfile profile,
            int topologyPopulationBasis,
            DeterministicRandom random)
        {
            decimal ratio = GetBaseHousingRatio(profile.PopulationOccupancyProfile) +
                            GetDensityHousingModifier(profile.UrbanDensity) +
                            GetDevelopmentHousingModifier(profile.DevelopmentLevel) +
                            GetFootprintHousingModifier(profile.SizeTier) +
                            random.NextDecimal(
                                minInclusive: -0.08m,
                                maxInclusive: 0.08m);

            ratio = Math.Clamp(
                value: ratio,
                min: 0.75m,
                max: 1.45m);

            return Math.Max(
                val1: 40,
                val2: (int)Math.Round(
                    d: topologyPopulationBasis * ratio,
                    mode: MidpointRounding.AwayFromZero));
        }

        private static decimal GetBaseHousingRatio(PopulationOccupancyProfile occupancyProfile)
        {
            return occupancyProfile switch
            {
                PopulationOccupancyProfile.Light => 1.28m,
                PopulationOccupancyProfile.High => 0.92m,
                _ => 1.08m
            };
        }

        private static decimal GetDensityHousingModifier(UrbanDensity urbanDensity)
        {
            return urbanDensity switch
            {
                UrbanDensity.Sparse => 0.08m,
                UrbanDensity.Dense => -0.05m,
                _ => 0.0m
            };
        }

        private static decimal GetDevelopmentHousingModifier(CityDevelopmentLevel developmentLevel)
        {
            return developmentLevel switch
            {
                CityDevelopmentLevel.Struggling => -0.07m,
                CityDevelopmentLevel.Advanced => 0.05m,
                _ => 0.0m
            };
        }

        private static decimal GetFootprintHousingModifier(CitySizeTier sizeTier)
        {
            return sizeTier switch
            {
                CitySizeTier.Small => -0.03m,
                CitySizeTier.Large => 0.04m,
                _ => 0.0m
            };
        }

        private static int ResolveDistrictCount(
            CityGenerationProfile profile,
            int targetResidentialCapacity)
        {
            int minimumDistricts = profile.SizeTier switch
            {
                CitySizeTier.Small => 3,
                CitySizeTier.Medium => 4,
                CitySizeTier.Large => 5,
                _ => 4
            };

            decimal baseCapacityPerDistrict = profile.SizeTier switch
            {
                CitySizeTier.Small => 1_200m,
                CitySizeTier.Medium => 2_500m,
                CitySizeTier.Large => 4_500m,
                _ => 2_500m
            };

            decimal densityFactor = profile.UrbanDensity switch
            {
                UrbanDensity.Sparse => 0.75m,
                UrbanDensity.Dense => 1.35m,
                _ => 1.0m
            };

            decimal developmentFactor = profile.DevelopmentLevel switch
            {
                CityDevelopmentLevel.Struggling => 0.90m,
                CityDevelopmentLevel.Advanced => 1.10m,
                _ => 1.0m
            };

            int districtCount = (int)Math.Ceiling(
                d: targetResidentialCapacity / (baseCapacityPerDistrict * densityFactor * developmentFactor));

            return Math.Clamp(
                value: districtCount,
                min: minimumDistricts,
                max: 36);
        }

        private static DistrictArchetype[] BuildDistrictArchetypes(
            CityGenerationProfile profile,
            int districtCount,
            DeterministicRandom random)
        {
            var archetypes = new DistrictArchetype[districtCount];

            for (int i = 0; i < districtCount; i++)
            {
                bool isCentral = i == 0;
                archetypes[i] = GetDistrictArchetype(
                    profile: profile,
                    isCentral: isCentral,
                    random: random);
            }

            return archetypes;
        }

        private static DistrictArchetype GetDistrictArchetype(
            CityGenerationProfile profile,
            bool isCentral,
            DeterministicRandom random)
        {
            if (isCentral)
            {
                if (profile.UrbanDensity == UrbanDensity.Dense)
                    return random.NextInt(
                               minInclusive: 0,
                               maxExclusive: 100) <
                           60
                        ? DistrictArchetype.VerticalCore
                        : DistrictArchetype.CivicCore;

                return random.NextInt(
                           minInclusive: 0,
                           maxExclusive: 100) <
                       75
                    ? DistrictArchetype.CivicCore
                    : DistrictArchetype.MixedBlocks;
            }

            if (profile.DevelopmentLevel == CityDevelopmentLevel.Struggling &&
                profile.UrbanDensity == UrbanDensity.Dense)
                return random.NextInt(
                           minInclusive: 0,
                           maxExclusive: 100) <
                       55
                    ? DistrictArchetype.DormitoryBelt
                    : DistrictArchetype.MixedBlocks;

            if (profile.UrbanDensity == UrbanDensity.Sparse)
                return random.NextInt(
                           minInclusive: 0,
                           maxExclusive: 100) <
                       70
                    ? DistrictArchetype.CottageRing
                    : DistrictArchetype.MixedBlocks;

            if (profile.UrbanDensity == UrbanDensity.Dense)
                return random.NextInt(
                           minInclusive: 0,
                           maxExclusive: 100) <
                       35
                    ? DistrictArchetype.VerticalCore
                    : random.NextInt(
                          minInclusive: 0,
                          maxExclusive: 100) <
                      50
                        ? DistrictArchetype.DormitoryBelt
                        : DistrictArchetype.MixedBlocks;

            return random.NextInt(
                       minInclusive: 0,
                       maxExclusive: 100) <
                   65
                ? DistrictArchetype.MixedBlocks
                : DistrictArchetype.CottageRing;
        }

        private static int[] BuildDistrictCapacityTargets(
            CityGenerationProfile profile,
            int totalCapacityTarget,
            int districtCount,
            DistrictArchetype[] districtArchetypes,
            DeterministicRandom random)
        {
            decimal[] weights = new decimal[districtCount];
            decimal weightTotal = 0.0m;

            for (int i = 0; i < districtCount; i++)
            {
                decimal weight = i == 0
                    ? profile.UrbanDensity == UrbanDensity.Dense
                        ? 1.45m
                        : 1.25m
                    : random.NextDecimal(
                        minInclusive: 0.80m,
                        maxInclusive: 1.30m);

                weight *= GetDistrictCapacityWeight(districtArchetypes[i]);

                if (profile.SizeTier == CitySizeTier.Large && i > 0)
                    weight += 0.05m;

                weights[i] = weight;
                weightTotal += weight;
            }

            int[] capacities = new int[districtCount];
            int allocated = 0;

            for (int i = 0; i < districtCount; i++)
            {
                decimal ratio = weights[i] / weightTotal;
                int minimumDistrictCapacity = i == 0
                    ? 250
                    : 120;
                capacities[i] = Math.Max(
                    val1: minimumDistrictCapacity,
                    val2: (int)Math.Round(
                        d: totalCapacityTarget * ratio,
                        mode: MidpointRounding.AwayFromZero));
                allocated += capacities[i];
            }

            int delta = totalCapacityTarget - allocated;
            if (delta != 0)
                capacities[^1] = Math.Max(
                    val1: 60,
                    val2: capacities[^1] + delta);

            return capacities;
        }

        private static decimal GetDistrictCapacityWeight(DistrictArchetype archetype)
        {
            return archetype switch
            {
                DistrictArchetype.CivicCore => 1.18m,
                DistrictArchetype.VerticalCore => 1.32m,
                DistrictArchetype.DormitoryBelt => 1.14m,
                DistrictArchetype.CottageRing => 0.86m,
                _ => 1.0m
            };
        }

        private static ResidentialBuildingType GetBuildingType(
            CityGenerationProfile profile,
            bool isCentral,
            DistrictArchetype archetype,
            DeterministicRandom random)
        {
            int houseWeight;
            int apartmentWeight;
            int towerWeight;
            int dormitoryWeight;

            switch (archetype)
            {
                case DistrictArchetype.CivicCore:
                    houseWeight = 6;
                    apartmentWeight = 42;
                    towerWeight = 42;
                    dormitoryWeight = 10;
                    break;
                case DistrictArchetype.VerticalCore:
                    houseWeight = 2;
                    apartmentWeight = 28;
                    towerWeight = 58;
                    dormitoryWeight = 12;
                    break;
                case DistrictArchetype.CottageRing:
                    houseWeight = 72;
                    apartmentWeight = 22;
                    towerWeight = 2;
                    dormitoryWeight = 4;
                    break;
                case DistrictArchetype.DormitoryBelt:
                    houseWeight = 4;
                    apartmentWeight = 30;
                    towerWeight = 18;
                    dormitoryWeight = 48;
                    break;
                default:
                    houseWeight = 25;
                    apartmentWeight = 50;
                    towerWeight = 20;
                    dormitoryWeight = 5;
                    break;
            }

            if (isCentral)
            {
                houseWeight = Math.Max(
                    val1: 2,
                    val2: houseWeight - 15);
                apartmentWeight += 5;
                towerWeight += 10;
            }

            if (profile.DevelopmentLevel == CityDevelopmentLevel.Struggling)
            {
                apartmentWeight += 10;
                towerWeight = Math.Max(
                    val1: 2,
                    val2: towerWeight - 10);
            }
            else
                if (profile.DevelopmentLevel == CityDevelopmentLevel.Advanced)
            {
                towerWeight += 10;
                houseWeight = Math.Max(
                    val1: 2,
                    val2: houseWeight - 5);
            }

            if (profile.SizeTier == CitySizeTier.Small && !isCentral)
                towerWeight = Math.Max(
                    val1: 1,
                    val2: towerWeight - 10);

            int roll = random.NextInt(
                minInclusive: 1,
                maxExclusive: houseWeight + apartmentWeight + towerWeight + dormitoryWeight + 1);

            if (roll <= houseWeight)
                return ResidentialBuildingType.House;

            roll -= houseWeight;
            if (roll <= apartmentWeight)
                return ResidentialBuildingType.ApartmentBlock;

            roll -= apartmentWeight;
            if (roll <= towerWeight)
                return ResidentialBuildingType.Tower;

            return ResidentialBuildingType.Dormitory;
        }

        private static int GetResidentCapacity(
            ResidentialBuildingType type,
            CityGenerationProfile profile,
            int topologyPopulationBasis,
            bool isCentral,
            DistrictArchetype archetype,
            DeterministicRandom random)
        {
            int minCapacity;
            int maxCapacity;

            switch (type)
            {
                case ResidentialBuildingType.House:
                    minCapacity = 4;
                    maxCapacity = 10;
                    break;
                case ResidentialBuildingType.Tower:
                    minCapacity = 180;
                    maxCapacity = 360;
                    break;
                case ResidentialBuildingType.Dormitory:
                    minCapacity = 120;
                    maxCapacity = 260;
                    break;
                default:
                    minCapacity = 70;
                    maxCapacity = 180;
                    break;
            }

            decimal densityFactor = profile.UrbanDensity switch
            {
                UrbanDensity.Sparse => 0.85m,
                UrbanDensity.Balanced => 1.0m,
                UrbanDensity.Dense => 1.2m,
                _ => 1.0m
            };

            decimal developmentFactor = profile.DevelopmentLevel switch
            {
                CityDevelopmentLevel.Struggling => 0.9m,
                CityDevelopmentLevel.Balanced => 1.0m,
                CityDevelopmentLevel.Advanced => 1.15m,
                _ => 1.0m
            };

            decimal centralFactor = isCentral
                ? 1.1m
                : 1.0m;
            decimal archetypeFactor = archetype switch
            {
                DistrictArchetype.CivicCore => 1.10m,
                DistrictArchetype.VerticalCore => 1.18m,
                DistrictArchetype.DormitoryBelt => 1.12m,
                DistrictArchetype.CottageRing => 0.78m,
                _ => 1.0m
            };
            decimal populationScale = GetPopulationCapacityScale(
                profile: profile,
                topologyPopulationBasis: topologyPopulationBasis);
            int rawCapacity = random.NextInt(
                minInclusive: minCapacity,
                maxExclusive: maxCapacity + 1);
            decimal adjustedCapacity = rawCapacity *
                                       densityFactor *
                                       developmentFactor *
                                       centralFactor *
                                       archetypeFactor *
                                       populationScale;

            return Math.Max(
                val1: ResidentCapacity.Min,
                val2: (int)Math.Round(
                    d: adjustedCapacity,
                    mode: MidpointRounding.AwayFromZero));
        }

        private static decimal GetPopulationCapacityScale(
            CityGenerationProfile profile,
            int topologyPopulationBasis)
        {
            decimal scale = topologyPopulationBasis switch
            {
                >= 100_000 => 1.35m,
                >= 10_000 => 1.18m,
                >= 1_000 => 1.05m,
                _ => 1.0m
            };

            if (profile.UrbanDensity == UrbanDensity.Dense)
                scale += 0.10m;
            else
                if (profile.UrbanDensity == UrbanDensity.Sparse)
                scale -= 0.05m;

            if (profile.DevelopmentLevel == CityDevelopmentLevel.Advanced)
                scale += 0.08m;
            else
                if (profile.DevelopmentLevel == CityDevelopmentLevel.Struggling)
                scale -= 0.05m;

            if (profile.SizeTier == CitySizeTier.Large)
                scale += 0.06m;
            else
                if (profile.SizeTier == CitySizeTier.Small)
                scale -= 0.03m;

            return Math.Clamp(
                value: scale,
                min: 0.85m,
                max: 1.80m);
        }

        private static string CreateBuildingName(
            string districtLabel,
            ResidentialBuildingType type,
            int sequence)
        {
            string typeLabel = type switch
            {
                ResidentialBuildingType.House => "House",
                ResidentialBuildingType.ApartmentBlock => "Block",
                ResidentialBuildingType.Tower => "Tower",
                ResidentialBuildingType.Dormitory => "Residence",
                _ => "Building"
            };

            return $"{districtLabel} {typeLabel} {sequence}";
        }

        private static ulong BuildSeed(City city)
        {
            string compositeSeed = string.Concat(
                city.GenerationSeed.Value,
                "|",
                city.GenerationProfile.SizeTier,
                "|",
                city.GenerationProfile.UrbanDensity,
                "|",
                city.GenerationProfile.DevelopmentLevel,
                "|",
                city.GenerationProfile.PopulationOccupancyProfile,
                "|",
                city.GenerationProfile.PlannedPeopleCount?.ToString() ?? "auto",
                "|",
                city.Environment.ClimateZone,
                "|",
                city.Environment.Hemisphere,
                "|",
                city.Environment.UtcOffset.TotalMinutes);

            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(compositeSeed));
            return BitConverter.ToUInt64(
                value: bytes,
                startIndex: 0);
        }

        private static void Shuffle<T>(
            IList<T> items,
            DeterministicRandom random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = random.NextInt(
                    minInclusive: 0,
                    maxExclusive: i + 1);
                T current = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = current;
            }
        }

        private sealed record GenerationPlan(
            int TargetPopulation,
            int TopologyPopulationBasis,
            int TargetResidentialCapacity,
            int DistrictCount);

        private sealed record ResidentialBuildingBlueprint(
            District District,
            string Name,
            ResidentialBuildingType Type,
            int ResidentCapacity,
            decimal PositionX,
            decimal PositionY,
            DateTimeOffset CreatedAtUtc);

        private sealed record CityAnchorBlueprint(
            District District,
            string Name,
            CityAnchorType Type,
            int Capacity,
            decimal PositionX,
            decimal PositionY,
            DateTimeOffset CreatedAtUtc);

        private sealed record RoadGraphLayout(
            IReadOnlyCollection<ResidentialBuilding> ResidentialBuildings,
            IReadOnlyCollection<CityAnchor> Anchors,
            IReadOnlyCollection<RoadNode> RoadNodes,
            IReadOnlyCollection<RoadSegment> RoadSegments);

        private enum DistrictArchetype
        {
            CivicCore,
            MixedBlocks,
            VerticalCore,
            CottageRing,
            DormitoryBelt
        }

        private sealed class DeterministicRandom
        {
            private ulong _state;

            public DeterministicRandom(ulong seed)
            {
                _state = seed == 0
                    ? 0x9E3779B97F4A7C15UL
                    : seed;
            }

            public int NextInt(
                int minInclusive,
                int maxExclusive)
            {
                if (maxExclusive <= minInclusive)
                    return minInclusive;

                ulong range = (ulong)(maxExclusive - minInclusive);
                ulong sample = NextUInt64() % range;
                return minInclusive + (int)sample;
            }

            public decimal NextDecimal(
                decimal minInclusive,
                decimal maxInclusive)
            {
                if (maxInclusive <= minInclusive)
                    return minInclusive;

                const decimal denominator = 1_000_000m;
                decimal normalized = NextInt(
                                         minInclusive: 0,
                                         maxExclusive: 1_000_001) /
                                     denominator;

                return minInclusive + ((maxInclusive - minInclusive) * normalized);
            }

            private ulong NextUInt64()
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
