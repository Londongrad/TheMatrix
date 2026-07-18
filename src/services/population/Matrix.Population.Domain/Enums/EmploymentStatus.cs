namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    ///     Employment status of a person:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="Unemployed" /> – not currently employed</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Employed" /> – has a paid job</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Retired" /> – no longer in the workforce due to retirement</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="None" /> – not specified or not applicable</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public enum EmploymentStatus
    {
        /// <summary>
        ///     Not currently employed.
        /// </summary>
        Unemployed = 0,

        /// <summary>
        ///     Has a paid job.
        /// </summary>
        Employed = 1,

        /// <summary>
        ///     No longer in the workforce due to retirement.
        /// </summary>
        Retired = 3,

        /// <summary>
        ///     Not specified or not applicable.
        /// </summary>
        None = 4
    }
}
