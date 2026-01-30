using System.Security.Cryptography;
using System.Text;
using Matrix.CityCore.Application.Services.Generation.Abstractions;
using Matrix.CityCore.Application.Services.Topology.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
using Matrix.CityCore.Domain.Topology;
using Matrix.CityCore.Domain.Topology.Enums;

namespace Matrix.CityCore.Application.Services.Topology
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
            DateTimeOffset createdAtUtc = city.CreatedAtUtc;

            List<District> districts = CreateDistricts(
                city: city,
                createdAtUtc: createdAtUtc,
                random: random);

            List<ResidentialBuilding> buildings = CreateResidentialBuildings(
                city: city,
                districts: districts,
                createdAtUtc: createdAtUtc,
                random: random);

            return new CityTopologySeed(
                Districts: districts,
                ResidentialBuildings: buildings);
        }

        private List<District> CreateDistricts(
            City city,
            DateTimeOffset createdAtUtc,
            DeterministicRandom random)
        {
            int districtCount = GetDistrictCount(
                profile: city.GenerationProfile,
                random: random);

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
                    createdAtUtc: createdAtUtc)
            };

            for (int i = 1; i < districtCount; i++)
            {
                string districtName = i - 1 < availableNames.Count
                    ? availableNames[i - 1]
                    : $"Sector {i}";

                districts.Add(
                    District.Create(
                        cityId: city.Id,
                        name: new DistrictName(districtName),
                        createdAtUtc: createdAtUtc));
            }

            return districts;
        }

        private static List<ResidentialBuilding> CreateResidentialBuildings(
            City city,
            IReadOnlyList<District> districts,
            DateTimeOffset createdAtUtc,
            DeterministicRandom random)
        {
            var buildings = new List<ResidentialBuilding>();

            for (int districtIndex = 0; districtIndex < districts.Count; districtIndex++)
            {
                District district = districts[districtIndex];
                bool isCentral = districtIndex == 0;
                int buildingCount = GetBuildingCount(
                    profile: city.GenerationProfile,
                    isCentral: isCentral,
                    random: random);

                string districtLabel = district.Name.Value.Replace(
                    oldValue: " District",
                    newValue: string.Empty,
                    comparisonType: StringComparison.Ordinal);
                var typeCounters = new Dictionary<ResidentialBuildingType, int>();

                for (int buildingIndex = 0; buildingIndex < buildingCount; buildingIndex++)
                {
                    ResidentialBuildingType type = GetBuildingType(
                        profile: city.GenerationProfile,
                        isCentral: isCentral,
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
                        isCentral: isCentral,
                        random: random);

                    buildings.Add(
                        ResidentialBuilding.Create(
                            cityId: city.Id,
                            districtId: district.Id,
                            name: new ResidentialBuildingName(
                                CreateBuildingName(
                                    districtLabel: districtLabel,
                                    type: type,
                                    sequence: sequence)),
                            type: type,
                            residentCapacity: ResidentCapacity.From(residentCapacity),
                            createdAtUtc: createdAtUtc));
                }
            }

            return buildings;
        }

        private static int GetDistrictCount(
            CityGenerationProfile profile,
            DeterministicRandom random)
        {
            int baseCount = profile.SizeTier switch
            {
                CitySizeTier.Small => 3,
                CitySizeTier.Medium => 5,
                CitySizeTier.Large => 7,
                _ => 5
            };

            int densityBonus = profile.UrbanDensity switch
            {
                UrbanDensity.Sparse => 0,
                UrbanDensity.Balanced => 1,
                UrbanDensity.Dense => 2,
                _ => 1
            };

            int developmentBonus = profile.DevelopmentLevel == CityDevelopmentLevel.Advanced
                ? 1
                : 0;

            int randomBonus = random.NextInt(
                minInclusive: 0,
                maxExclusive: densityBonus + 1);

            return Math.Min(
                val1: 10,
                val2: baseCount + developmentBonus + randomBonus);
        }

        private static int GetBuildingCount(
            CityGenerationProfile profile,
            bool isCentral,
            DeterministicRandom random)
        {
            int baseCount = profile.UrbanDensity switch
            {
                UrbanDensity.Sparse => 2,
                UrbanDensity.Balanced => 3,
                UrbanDensity.Dense => 4,
                _ => 3
            };

            int sizeBonus = profile.SizeTier switch
            {
                CitySizeTier.Small => 0,
                CitySizeTier.Medium => 1,
                CitySizeTier.Large => 2,
                _ => 1
            };

            int centralBonus = isCentral
                ? 2
                : 0;
            int developmentBonus = profile.DevelopmentLevel == CityDevelopmentLevel.Advanced
                ? 1
                : 0;
            int randomBonus = random.NextInt(
                minInclusive: 0,
                maxExclusive: 2);

            return Math.Min(
                val1: 9,
                val2: baseCount + sizeBonus + centralBonus + developmentBonus + randomBonus);
        }

        private static ResidentialBuildingType GetBuildingType(
            CityGenerationProfile profile,
            bool isCentral,
            DeterministicRandom random)
        {
            int houseWeight;
            int apartmentWeight;
            int towerWeight;
            int dormitoryWeight;

            switch (profile.UrbanDensity)
            {
                case UrbanDensity.Sparse:
                    houseWeight = 60;
                    apartmentWeight = 30;
                    towerWeight = 5;
                    dormitoryWeight = 5;
                    break;
                case UrbanDensity.Dense:
                    houseWeight = 10;
                    apartmentWeight = 45;
                    towerWeight = 35;
                    dormitoryWeight = 10;
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
            bool isCentral,
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
            int rawCapacity = random.NextInt(
                minInclusive: minCapacity,
                maxExclusive: maxCapacity + 1);
            decimal adjustedCapacity = rawCapacity * densityFactor * developmentFactor * centralFactor;

            return Math.Max(
                val1: ResidentCapacity.Min,
                val2: (int)Math.Round(
                    d: adjustedCapacity,
                    mode: MidpointRounding.AwayFromZero));
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
