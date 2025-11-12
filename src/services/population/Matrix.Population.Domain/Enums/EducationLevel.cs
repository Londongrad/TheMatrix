namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    /// Education level of a person:
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="None"/> – no formal education</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Primary"/> – completed primary / elementary school</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Secondary"/> – completed secondary / high school</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Vocational"/> – vocational or trade education after school</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Higher"/> – higher education (e.g. bachelor’s degree)</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Postgraduate"/> – postgraduate education (e.g. master’s, PhD)</description>
    ///   </item>
    /// </list>
    /// </summary>
    public enum EducationLevel
    {
        /// <summary>
        /// No formal education.
        /// </summary>
        None = 0,

        /// <summary>
        /// Completed primary / elementary school.
        /// </summary>
        Primary = 1,

        /// <summary>
        /// Completed secondary / high school.
        /// </summary>
        Secondary = 2,

        /// <summary>
        /// Vocational or trade education after school.
        /// </summary>
        Vocational = 3,

        /// <summary>
        /// Higher education (e.g. bachelor’s degree).
        /// </summary>
        Higher = 4,

        /// <summary>
        /// Postgraduate education (e.g. master’s, PhD).
        /// </summary>
        Postgraduate = 5
    }
}
