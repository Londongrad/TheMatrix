using System.Security.Cryptography;
using System.Text;
using Matrix.CityCore.Application.Services.Generation.Abstractions;

namespace Matrix.CityCore.Application.Services.Generation
{
    /// <summary>
    ///     Produces deterministic city name suggestions from the generation catalog.
    /// </summary>
    public sealed class CityNameSuggestionService(ICityGenerationContentCatalog contentCatalog)
        : ICityNameSuggestionService
    {
        public IReadOnlyList<string> GetSuggestions(
            string? seed,
            int count)
        {
            int effectiveCount = Math.Max(
                val1: 1,
                val2: count);
            var names = contentCatalog.CityNamePresets
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToList();

            if (names.Count == 0)
                return Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(seed))
                return names.Take(
                        Math.Min(
                            val1: effectiveCount,
                            val2: names.Count))
                   .ToArray();

            var random = new DeterministicRandom(BuildSeed(seed));
            Shuffle(
                items: names,
                random: random);

            return names.Take(
                    Math.Min(
                        val1: effectiveCount,
                        val2: names.Count))
               .ToArray();
        }

        private static ulong BuildSeed(string seed)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed.Trim()));
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
