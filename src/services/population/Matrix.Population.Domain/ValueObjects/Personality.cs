using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class Personality
    {
        #region [ Properties ]

        // 0..100, where 50 = neutral, 0 = low, 100 = very high
        public int Optimism { get; }
        public int Discipline { get; }
        public int RiskTolerance { get; }
        public int Sociability { get; }

        #endregion [ Properties ]

        #region [ Constructors ]

        private Personality(int optimism, int discipline, int riskTolerance, int sociability)
        {
            Optimism = GuardHelper.AgainstOutOfRange(optimism, 0, 100, nameof(Optimism));
            Discipline = GuardHelper.AgainstOutOfRange(discipline, 0, 100, nameof(Discipline));
            RiskTolerance = GuardHelper.AgainstOutOfRange(riskTolerance, 0, 100, nameof(RiskTolerance));
            Sociability = GuardHelper.AgainstOutOfRange(sociability, 0, 100, nameof(Sociability));
        }

        #endregion [ Constructors ]

        #region [ Factory Methods ]

        public static Personality Create(
            int optimism,
            int discipline,
            int riskTolerance,
            int sociability)
        {
            return new(optimism, discipline, riskTolerance, sociability);
        }

        /// <summary>
        /// Neutral personality with all traits set to 50.
        /// </summary>
        public static Personality Neutral()
        {

            return new(
                optimism: 50,
                discipline: 50,
                riskTolerance: 50,
                sociability: 50
            );
        }

        /// <summary>
        /// Creates a random trait value within the specified range depends on <see cref="PersonalityArchetype"/>.
        /// <para>
        /// Random comes from outside to ensure better randomness when creating multiple personalities.
        /// </summary>
        public static Personality CreateRandom(
            Random random,
            PersonalityArchetype archetype = PersonalityArchetype.Balanced)
        {
            ArgumentNullException.ThrowIfNull(random);

            return archetype switch
            {
                PersonalityArchetype.Optimist => new Personality(
                    optimism: NextTrait(random, 70, 100),
                    discipline: NextTrait(random, 40, 80),
                    riskTolerance: NextTrait(random, 40, 80),
                    sociability: NextTrait(random, 40, 90)),

                PersonalityArchetype.Pessimist => new Personality(
                    optimism: NextTrait(random, 0, 40),
                    discipline: NextTrait(random, 40, 80),
                    riskTolerance: NextTrait(random, 10, 60),
                    sociability: NextTrait(random, 10, 70)),

                PersonalityArchetype.RiskTaker => new Personality(
                    optimism: NextTrait(random, 40, 90),
                    discipline: NextTrait(random, 20, 70),
                    riskTolerance: NextTrait(random, 70, 100),
                    sociability: NextTrait(random, 30, 90)),

                PersonalityArchetype.Cautious => new Personality(
                    optimism: NextTrait(random, 30, 70),
                    discipline: NextTrait(random, 50, 100),
                    riskTolerance: NextTrait(random, 0, 40),
                    sociability: NextTrait(random, 20, 70)),

                PersonalityArchetype.Workaholic => new Personality(
                    optimism: NextTrait(random, 40, 80),
                    discipline: NextTrait(random, 70, 100),
                    riskTolerance: NextTrait(random, 30, 70),
                    sociability: NextTrait(random, 10, 60)),

                PersonalityArchetype.SocialButterfly => new Personality(
                    optimism: NextTrait(random, 50, 100),
                    discipline: NextTrait(random, 30, 70),
                    riskTolerance: NextTrait(random, 40, 80),
                    sociability: NextTrait(random, 70, 100)),

                _ => new Personality( // Balanced
                    optimism: NextTrait(random, 40, 60),
                    discipline: NextTrait(random, 40, 60),
                    riskTolerance: NextTrait(random, 40, 60),
                    sociability: NextTrait(random, 40, 60))
            };
        }

        #endregion [ Factory Methods ]

        #region [ Methods ]

        /// <summary>
        /// Adjusts happiness delta based on character traits.
        /// </summary>
        public int ModifyHappinessDelta(int baseDelta)
        {
            if (baseDelta == 0)
                return 0;

            // Нормализуем оптимизм: -1..+1
            decimal normalized = (Optimism - 50) / 50m; // 0 -> -1, 50 -> 0, 100 -> +1

            // 0.5..1.5 – насколько сильно человек переживает события
            decimal factor = 1m + normalized * 0.5m;

            var modified = (int)Math.Round(baseDelta * factor);

            // Если эмоция была, но округление убило дельту — вернём хотя бы 1/-1
            if (modified == 0)
                modified = baseDelta > 0 ? 1 : -1;

            return modified;
        }

        private static int NextTrait(Random random, int minInclusive, int maxInclusive)
        {
            // Random.Next верхнюю границу не включает, поэтому +1
            return random.Next(minInclusive, maxInclusive + 1);
        }

        #endregion [ Methods ] 
    }
}
