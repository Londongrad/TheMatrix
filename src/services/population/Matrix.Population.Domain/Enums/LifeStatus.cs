namespace Matrix.Population.Domain.Enums
{
    /// <summary>
    ///     Life status of a person:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="Alive" /> – person is currently alive</description>
    ///         </item>
    ///         <item>
    ///             <description><see cref="Deceased" /> – person has died</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public enum LifeStatus
    {
        /// <summary>
        ///     Person is currently alive.
        /// </summary>
        Alive = 1,

        /// <summary>
        ///     Person has died.
        /// </summary>
        Deceased = 2
    }
}
