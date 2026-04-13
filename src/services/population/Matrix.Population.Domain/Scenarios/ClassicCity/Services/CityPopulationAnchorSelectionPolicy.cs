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
            Guid stableKey,
            IReadOnlyCollection<CityAnchorId>? preferredAnchorIds = null)
        {
            return SelectByType(
                anchors: anchors,
                type: CityAnchorType.Workplace,
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey,
                salt: 101,
                preferredAnchorIds: preferredAnchorIds);
        }

        public CityPopulationAnchorCatalogItem? SelectSchoolAnchor(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            DistrictId? preferredDistrictId,
            Guid stableKey,
            IReadOnlyCollection<CityAnchorId>? preferredAnchorIds = null)
        {
            return SelectByType(
                anchors: anchors,
                type: CityAnchorType.School,
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey,
                salt: 211,
                preferredAnchorIds: preferredAnchorIds);
        }

        public CityPopulationAnchorCatalogItem? SelectHospitalAnchor(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            DistrictId? preferredDistrictId,
            Guid stableKey,
            IReadOnlyCollection<CityAnchorId>? preferredAnchorIds = null)
        {
            return SelectByType(
                anchors: anchors,
                type: CityAnchorType.Hospital,
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey,
                salt: 307,
                preferredAnchorIds: preferredAnchorIds);
        }

        private static CityPopulationAnchorCatalogItem? SelectByType(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> anchors,
            CityAnchorType type,
            DistrictId? preferredDistrictId,
            Guid stableKey,
            int salt,
            IReadOnlyCollection<CityAnchorId>? preferredAnchorIds)
        {
            IReadOnlyList<CityPopulationAnchorCatalogItem> typedAnchors = anchors
               .Where(x => x.Type == type)
               .OrderBy(x => x.DistrictId.Value)
               .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
               .ThenBy(x => x.CityAnchorId.Value)
               .ToArray();

            if (typedAnchors.Count == 0)
                return null;

            IReadOnlyList<CityPopulationAnchorCatalogItem> preferredByRouteAnchors =
                ResolvePreferredAnchors(
                    typedAnchors: typedAnchors,
                    preferredAnchorIds: preferredAnchorIds);
            IReadOnlyList<CityPopulationAnchorCatalogItem> preferredAnchors = preferredDistrictId.HasValue
                ? typedAnchors.Where(x => x.DistrictId == preferredDistrictId.Value)
                   .ToArray()
                : [];

            IReadOnlyList<CityPopulationAnchorCatalogItem> effectiveAnchors = preferredByRouteAnchors.Count > 0
                ? preferredByRouteAnchors
                : preferredAnchors.Count > 0
                ? preferredAnchors
                : typedAnchors;

            int candidateCount = Math.Min(
                val1: effectiveAnchors.Count,
                val2: preferredByRouteAnchors.Count > 0
                    ? 3
                    : effectiveAnchors.Count);
            int index = GetStableIndex(
                stableKey: stableKey,
                salt: salt,
                modulus: candidateCount);

            return effectiveAnchors[index];
        }

        private static IReadOnlyList<CityPopulationAnchorCatalogItem> ResolvePreferredAnchors(
            IReadOnlyList<CityPopulationAnchorCatalogItem> typedAnchors,
            IReadOnlyCollection<CityAnchorId>? preferredAnchorIds)
        {
            if (preferredAnchorIds is null || preferredAnchorIds.Count == 0)
                return [];

            var anchorsById = typedAnchors.ToDictionary(
                keySelector: x => x.CityAnchorId,
                elementSelector: x => x);
            List<CityPopulationAnchorCatalogItem> preferredAnchors = [];

            foreach (CityAnchorId preferredAnchorId in preferredAnchorIds)
                if (anchorsById.TryGetValue(
                        key: preferredAnchorId,
                        value: out CityPopulationAnchorCatalogItem? anchor))
                    preferredAnchors.Add(anchor);

            return preferredAnchors;
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
