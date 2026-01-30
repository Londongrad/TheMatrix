using Matrix.CityCore.Application.Services.Generation.Abstractions;

namespace Matrix.CityCore.Application.Services.Generation
{
    /// <summary>
    ///     Provides reusable in-memory content pools for deterministic city generation.
    /// </summary>
    public sealed class CityGenerationContentCatalog : ICityGenerationContentCatalog
    {
        private static readonly IReadOnlyList<string> CityNamePresetsInternal = new List<string>
        {
            "Alderhaven",
            "Amberfall",
            "Ashbourne",
            "Blackridge",
            "Blueharbor",
            "Brighton Vale",
            "Brookhollow",
            "Cedarpoint",
            "Clearwater",
            "Copperfield",
            "Dawnmere",
            "Driftmoor",
            "Eastwatch",
            "Eboncrest",
            "Emberfield",
            "Everford",
            "Fairharbor",
            "Frostford",
            "Glassport",
            "Goldmere",
            "Granite Bay",
            "Greenhollow",
            "Greyhaven",
            "Highgarden",
            "Highwatch",
            "Ironford",
            "Ironhaven",
            "Kingsbridge",
            "Larkspur",
            "Limeport",
            "Lowell Reach",
            "Mapleford",
            "Moonfall",
            "Northpass",
            "Oakridge",
            "Quartz Bay",
            "Redwater",
            "Rivermoor",
            "Roseford",
            "Seabrook",
            "Silverport",
            "Southgate",
            "Stonebridge",
            "Stonemere",
            "Sunreach",
            "Thornfield",
            "Westhaven",
            "Whitepeak",
            "Willowdale",
            "Windrest"
        }.AsReadOnly();

        private static readonly IReadOnlyList<string> DistrictNamePresetsInternal = new List<string>
        {
            "North District",
            "South District",
            "East District",
            "West District",
            "Old Town District",
            "Riverside District",
            "Garden District",
            "Market District",
            "Hillside District",
            "Station District",
            "University District",
            "Mill District",
            "Foundry District",
            "Lakeside District",
            "Harbor District",
            "Orchard District",
            "Heights District",
            "Westgate District",
            "Amber District",
            "Bridge District",
            "Canal District",
            "Civic District",
            "Crescent District",
            "Elm District",
            "Grand District",
            "Northgate District",
            "Park District",
            "Riverbend District",
            "Southbank District",
            "Summit District"
        }.AsReadOnly();

        private static readonly IReadOnlyList<string> StreetNamePresetsInternal = new List<string>
        {
            "Alder Street",
            "Amber Avenue",
            "Baker Street",
            "Birch Lane",
            "Bridge Street",
            "Canal Avenue",
            "Cedar Road",
            "Central Avenue",
            "Cherry Street",
            "Copper Street",
            "Crescent Road",
            "Elm Street",
            "Foundry Street",
            "Garden Lane",
            "Granite Road",
            "Harbor Road",
            "Hill Street",
            "King Street",
            "Lake Avenue",
            "Lantern Street",
            "Linden Road",
            "Maple Street",
            "Market Street",
            "Mill Road",
            "North Avenue",
            "Oak Street",
            "Orchard Lane",
            "Park Avenue",
            "Quartz Street",
            "Railway Street",
            "River Street",
            "Rose Lane",
            "Silver Avenue",
            "South Avenue",
            "Station Road",
            "Stone Street",
            "Sunset Road",
            "Union Street",
            "Valley Road",
            "Willow Lane"
        }.AsReadOnly();

        public IReadOnlyList<string> CityNamePresets => CityNamePresetsInternal;
        public IReadOnlyList<string> DistrictNamePresets => DistrictNamePresetsInternal;
        public IReadOnlyList<string> StreetNamePresets => StreetNamePresetsInternal;
    }
}
