namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    ///     Marital status of a person:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="Single" /> – not married</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Married" /> – currently married</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Divorced" /> – marriage legally dissolved</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Widowed" /> – spouse has died</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public enum MaritalStatus
    {
        /// <summary>
        ///     Not married.
        /// </summary>
        Single = 1,

        /// <summary>
        ///     Currently married.
        /// </summary>
        Married = 2,

        /// <summary>
        ///     Marriage legally dissolved.
        /// </summary>
        Divorced = 3,

        /// <summary>
        ///     Spouse has died.
        /// </summary>
        Widowed = 4
    }
}
