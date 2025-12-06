namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    ///     Education level of a person:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="None" /> – no formal education</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Preschool" /> – preschool / kindergarten (детский сад)</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Primary" /> – completed primary / elementary school</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="LowerSecondary" /> – lower secondary / middle school (e.g. grades 5–9)</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="UpperSecondary" /> – upper secondary / high school (e.g. grades 10–11/12)</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Vocational" /> – vocational / trade education after school (college, техникум)</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Higher" /> – higher education (e.g. bachelor’s/specialist degree)</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Postgraduate" /> – postgraduate education (e.g. master’s, PhD, аспирантура)</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public enum EducationLevel
    {
        /// <summary>
        ///     No formal education (too young or never attended any institution).
        /// </summary>
        None = 0,

        /// <summary>
        ///     Preschool / kindergarten level (детский сад, дошкольное образование).
        /// </summary>
        Preschool = 1,

        /// <summary>
        ///     Completed primary / elementary school.
        /// </summary>
        Primary = 2,

        /// <summary>
        ///     Lower secondary / middle school (e.g. grades 5–9).
        /// </summary>
        LowerSecondary = 3,

        /// <summary>
        ///     Upper secondary / high school (e.g. grades 10–11).
        /// </summary>
        UpperSecondary = 4,

        /// <summary>
        ///     Vocational or trade education after school (college, техникум).
        /// </summary>
        Vocational = 5,

        /// <summary>
        ///     Higher education (e.g. bachelor's degree, specialist).
        /// </summary>
        Higher = 6,

        /// <summary>
        ///     Postgraduate education (e.g. master's degree, PhD, аспирантура).
        /// </summary>
        Postgraduate = 7
    }
}
