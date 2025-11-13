namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    /// Age group of a person.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Value</term>
    ///     <description>Age range</description>
    ///   </listheader>
    ///   <item>
    ///     <term><see cref="Child"/></term>
    ///     <description>0–6 years</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="Youth"/></term>
    ///     <description>7–22 years</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="Adult"/></term>
    ///     <description>23–65 years</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="Senior"/></term>
    ///     <description>66+ years</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public enum AgeGroup
    {
        /// <summary>
        /// 0–6
        /// </summary>
        Child,

        /// <summary>
        /// 7–22
        /// </summary>
        Youth,

        /// <summary>
        /// 23–65
        /// </summary>
        Adult,

        /// <summary>
        /// 66+
        /// </summary>
        Senior
    }
}
