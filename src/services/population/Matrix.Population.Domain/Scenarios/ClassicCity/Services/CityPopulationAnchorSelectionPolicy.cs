using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationAnchorSelectionPolicy
    {
        public CityPopulationAnchorCatalogItem? SelectWorkplaceAnchor(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            DistrictId? preferredDistrictId,
            Guid stableKey)
        {
            return SelectByType(
                anchors: anchors,
                type: CityAnchorType.Workplace,
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey,
                salt: 101);
        }

        public CityPopulationAnchorCatalogItem? SelectSchoolAnchor(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            DistrictId? preferredDistrictId,
            Guid stableKey)
        {
            return SelectByType(
                anchors: anchors,
                type: CityAnchorType.School,
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey,
                salt: 211);
        }

        public CityPopulationAnchorCatalogItem? SelectHospitalAnchor(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            DistrictId? preferredDistrictId,
            Guid stableKey)
        {
            return SelectByType(
                anchors: anchors,
                type: CityAnchorType.Hospital,
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey,
                salt: 307);
        }

        private static CityPopulationAnchorCatalogItem? SelectByType(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            CityAnchorType type,
            DistrictId? preferredDistrictId,
            Guid stableKey,
            int salt)
        {
            IReadOnlyList<CityPopulationAnchorCatalogItem> typedAnchors = anchors
               .Where(x => x.Type == type)
               .OrderBy(x => x.DistrictId.Value)
               .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
               .ThenBy(x => x.CityAnchorId.Value)
               .ToArray();

            if (typedAnchors.Count == 0)
                return null;

            IReadOnlyList<CityPopulationAnchorCatalogItem> preferredAnchors = preferredDistrictId.HasValue
                ? typedAnchors.Where(x => x.DistrictId == preferredDistrictId.Value)
                   .ToArray()
                : [];

            IReadOnlyList<CityPopulationAnchorCatalogItem> effectiveAnchors = preferredAnchors.Count > 0
                ? preferredAnchors
                : typedAnchors;

            int index = GetStableIndex(
                stableKey: stableKey,
                salt: salt,
                modulus: effectiveAnchors.Count);

            return effectiveAnchors[index];
        }

        private static int GetStableIndex(
            Guid stableKey,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            unchecked
            {
                byte[] bytes = stableKey.ToByteArray();
                int hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
        }
    }
}
