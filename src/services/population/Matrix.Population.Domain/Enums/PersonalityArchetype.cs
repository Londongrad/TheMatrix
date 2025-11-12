namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    /// Personality archetype of a person:
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="Balanced"/> – mixes optimism and caution, takes moderate risks</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Optimist"/> – focuses on positive outcomes and expects things to work out</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Pessimist"/> – tends to expect problems and negative outcomes</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="RiskTaker"/> – accepts high risk for potentially high reward</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Cautious"/> – avoids risk and prefers safe, predictable choices</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Workaholic"/> – strongly focused on work and productivity</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="SocialButterfly"/> – seeks frequent social contact and interaction</description>
    ///   </item>
    /// </list>
    /// </summary>
    public enum PersonalityArchetype
    {
        /// <summary>
        /// Mixes optimism and caution, takes moderate risks.
        /// </summary>
        Balanced = 0,

        /// <summary>
        /// Focuses on positive outcomes and expects things to work out.
        /// </summary>
        Optimist = 1,

        /// <summary>
        /// Tends to expect problems and negative outcomes.
        /// </summary>
        Pessimist = 2,

        /// <summary>
        /// Accepts high risk for potentially high reward.
        /// </summary>
        RiskTaker = 3,

        /// <summary>
        /// Avoids risk and prefers safe, predictable choices.
        /// </summary>
        Cautious = 4,

        /// <summary>
        /// Strongly focused on work and productivity.
        /// </summary>
        Workaholic = 5,

        /// <summary>
        /// Seeks frequent social contact and interaction.
        /// </summary>
        SocialButterfly = 6
    }
}
