using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Progression
{
    public sealed class ClassicCityEducationInstitutionSelectionPolicy
    {
        public EducationInstitution? TryReserveInstitution(
            ResidentId residentId,
            EducationStageKey stage,
            IReadOnlyCollection<EducationInstitution> institutions)
        {
            ArgumentNullException.ThrowIfNull(institutions);

            EducationInstitution[] candidates = institutions
               .Where(institution => institution.IsActive &&
                                     institution.AvailableSeatCount > 0 &&
                                     SupportsStage(institution.Kind, stage))
               .OrderBy(institution => institution.EducationInstitutionId.Value)
               .ToArray();
            if (candidates.Length == 0)
                return null;

            int startIndex = ResolveStableStartIndex(residentId, candidates.Length);
            for (int offset = 0; offset < candidates.Length; offset++)
            {
                EducationInstitution candidate = candidates[(startIndex + offset) % candidates.Length];
                if (candidate.TryReserveSeats(1))
                    return candidate;
            }

            return null;
        }

        private static bool SupportsStage(
            EducationInstitutionKindKey institutionKind,
            EducationStageKey stage)
        {
            bool isPostSecondary = stage == ClassicCityEducationStageCatalog.Vocational ||
                                   stage == ClassicCityEducationStageCatalog.Higher ||
                                   stage == ClassicCityEducationStageCatalog.Postgraduate;
            string kind = institutionKind.Value;

            return isPostSecondary
                ? string.Equals(kind, "university", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(kind, "college", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(kind, "academy", StringComparison.OrdinalIgnoreCase)
                : string.Equals(kind, "school", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(kind, "kindergarten", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(kind, "general", StringComparison.OrdinalIgnoreCase);
        }

        private static int ResolveStableStartIndex(ResidentId residentId, int candidateCount)
        {
            Span<byte> bytes = stackalloc byte[16];
            residentId.Value.TryWriteBytes(bytes);
            uint value = (uint)(bytes[0] |
                                (bytes[1] << 8) |
                                (bytes[2] << 16) |
                                (bytes[3] << 24));
            return (int)(value % (uint)candidateCount);
        }
    }
}
