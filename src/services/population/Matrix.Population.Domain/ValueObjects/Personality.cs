using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class Personality
    {
        private const int TraitMinValue = 0;
        private const int TraitMaxValue = 100;

        #region [ Constructors ]

        private Personality(
            int optimism,
            int discipline,
            int riskTolerance,
            int sociability)
        {
            Optimism = GuardHelper.AgainstOutOfRange(
                value: optimism,
                min: TraitMinValue,
                max: TraitMaxValue,
                propertyName: nameof(Optimism));
            Discipline = GuardHelper.AgainstOutOfRange(
                value: discipline,
                min: TraitMinValue,
                max: TraitMaxValue,
                propertyName: nameof(Discipline));
            RiskTolerance = GuardHelper.AgainstOutOfRange(
                value: riskTolerance,
                min: TraitMinValue,
                max: TraitMaxValue,
                propertyName: nameof(RiskTolerance));
            Sociability = GuardHelper.AgainstOutOfRange(
                value: sociability,
                min: TraitMinValue,
                max: TraitMaxValue,
                propertyName: nameof(Sociability));
        }

        #endregion [ Constructors ]

        #region [ Properties ]

        // 0..100, where 50 = neutral, 0 = low, 100 = very high
        public int Optimism { get; }
        public int Discipline { get; }
        public int RiskTolerance { get; }
        public int Sociability { get; }

        #endregion [ Properties ]

        #region [ Factory Methods ]

        public static Personality Create(
            int optimism,
            int discipline,
            int riskTolerance,
            int sociability)
        {
            return new Personality(
                optimism: optimism,
                discipline: discipline,
                riskTolerance: riskTolerance,
                sociability: sociability);
        }

        /// <summary>
        ///     Neutral personality with all traits set to 50
        ///     (does not amplify or dampen happiness changes).
        /// </summary>
        public static Personality Neutral()
        {
            return new Personality(
                optimism: 50,
                discipline: 50,
                riskTolerance: 50,
                sociability: 50);
        }

        /// <summary>
        ///     Creates a random trait value within the specified range depends on <see cref="PersonalityArchetype" />.
        ///     <para>
        ///         Random comes from outside to ensure better randomness when creating multiple personalities.
        /// </summary>
        public static Personality CreateRandom(
            Random random,
            PersonalityArchetype archetype = PersonalityArchetype.Balanced)
        {
            ArgumentNullException.ThrowIfNull(random);

            return archetype switch
            {
                PersonalityArchetype.Optimist => new Personality(
                    optimism: NextTrait(
                        random: random,
                        minInclusive: 70,
                        maxInclusive: 100),
                    discipline: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 80),
                    riskTolerance: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 80),
                    sociability: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 90)),

                PersonalityArchetype.Pessimist => new Personality(
                    optimism: NextTrait(
                        random: random,
                        minInclusive: 0,
                        maxInclusive: 40),
                    discipline: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 80),
                    riskTolerance: NextTrait(
                        random: random,
                        minInclusive: 10,
                        maxInclusive: 60),
                    sociability: NextTrait(
                        random: random,
                        minInclusive: 10,
                        maxInclusive: 70)),

                PersonalityArchetype.RiskTaker => new Personality(
                    optimism: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 90),
                    discipline: NextTrait(
                        random: random,
                        minInclusive: 20,
                        maxInclusive: 70),
                    riskTolerance: NextTrait(
                        random: random,
                        minInclusive: 70,
                        maxInclusive: 100),
                    sociability: NextTrait(
                        random: random,
                        minInclusive: 30,
                        maxInclusive: 90)),

                PersonalityArchetype.Cautious => new Personality(
                    optimism: NextTrait(
                        random: random,
                        minInclusive: 30,
                        maxInclusive: 70),
                    discipline: NextTrait(
                        random: random,
                        minInclusive: 50,
                        maxInclusive: 100),
                    riskTolerance: NextTrait(
                        random: random,
                        minInclusive: 0,
                        maxInclusive: 40),
                    sociability: NextTrait(
                        random: random,
                        minInclusive: 20,
                        maxInclusive: 70)),

                PersonalityArchetype.Workaholic => new Personality(
                    optimism: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 80),
                    discipline: NextTrait(
                        random: random,
                        minInclusive: 70,
                        maxInclusive: 100),
                    riskTolerance: NextTrait(
                        random: random,
                        minInclusive: 30,
                        maxInclusive: 70),
                    sociability: NextTrait(
                        random: random,
                        minInclusive: 10,
                        maxInclusive: 60)),

                PersonalityArchetype.SocialButterfly => new Personality(
                    optimism: NextTrait(
                        random: random,
                        minInclusive: 50,
                        maxInclusive: 100),
                    discipline: NextTrait(
                        random: random,
                        minInclusive: 30,
                        maxInclusive: 70),
                    riskTolerance: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 80),
                    sociability: NextTrait(
                        random: random,
                        minInclusive: 70,
                        maxInclusive: 100)),

                _ => new Personality( // Balanced
                    optimism: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 60),
                    discipline: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 60),
                    riskTolerance: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 60),
                    sociability: NextTrait(
                        random: random,
                        minInclusive: 40,
                        maxInclusive: 60))
            };
        }

        #endregion [ Factory Methods ]

        #region [ Methods ]

        /// <summary>
        ///     Adjusts happiness delta based on character traits.
        /// </summary>
        public int ModifyHappinessDelta(int baseDelta)
        {
            if (baseDelta == 0)
                return 0;

            // Нормализуем оптимизм: -1..+1
            decimal normalized = (Optimism - 50) / 50m; // 0 -> -1, 50 -> 0, 100 -> +1

            // 0.5..1.5 – насколько сильно человек переживает события
            decimal factor = 1m + (normalized * 0.5m);

            int modified = (int)Math.Round(baseDelta * factor);

            // Если эмоция была, но округление убило дельту — вернём хотя бы 1/-1
            if (modified == 0)
                modified = baseDelta > 0
                    ? 1
                    : -1;

            return modified;
        }

        /// <summary>
        ///     Возвращает множитель склонности идти на риск (0.5..1.5).
        /// </summary>
        public decimal GetRiskFactor()
        {
            decimal normalized = (RiskTolerance - 50) / 50m; // -1..+1
            return 1m + (normalized * 0.5m); // 0.5..1.5
        }

        /// <summary>
        ///     Насколько человек склонен соблюдать правила (0..1).
        /// </summary>
        public decimal GetComplianceLevel()
        {
            // Высокая дисциплина → ближе к 1, низкая → ближе к 0
            return Discipline / 100m;
        }

        /// <summary>
        ///     Множитель на количество социальных контактов (0.5..1.5).
        /// </summary>
        public decimal GetContactsFactor()
        {
            decimal normalized = (Sociability - 50) / 50m;
            return 1m + (normalized * 0.5m);
        }

        #region [ Helpers ]

        private static int NextTrait(
            Random random,
            int minInclusive,
            int maxInclusive)
        {
            // Random.Next верхнюю границу не включает, поэтому +1
            return random.Next(
                minValue: minInclusive,
                maxValue: maxInclusive + 1);
        }

        #endregion [ Helpers ]

        #endregion [ Methods ]
    }
}
