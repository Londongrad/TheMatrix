namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    /// Employment status of a person:
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="Unemployed"/> – not currently employed</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Employed"/> – has a paid job</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Student"/> – main activity is studying</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="Retired"/> – no longer in the workforce due to retirement</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="None"/> – not specified or not applicable</description>
    ///   </item>
    /// </list>
    /// </summary>
    public enum EmploymentStatus
    {
        /// <summary>
        /// Not currently employed.
        /// </summary>
        Unemployed,

        /// <summary>
        /// Has a paid job.
        /// </summary>
        Employed,

        /// <summary>
        /// Main activity is studying.
        /// </summary>
        Student,

        /// <summary>
        /// No longer in the workforce due to retirement.
        /// </summary>
        Retired,

        /// <summary>
        /// Not specified or not applicable.
        /// </summary>
        None
    }
}
