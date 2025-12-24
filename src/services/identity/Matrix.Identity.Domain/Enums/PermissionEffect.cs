namespace Matrix.Identity.Domain.Enums
{
    /// <summary>
    /// Defines how a permission entry affects access evaluation.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>
    ///     <term><see cref="PermissionEffect.Allow"/></term>
    ///     <description>Grants the specified permission.</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="PermissionEffect.Deny"/></term>
    ///     <description>Explicitly blocks the specified permission (typically overrides an allow).</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public enum PermissionEffect : byte
    {
        Allow = 1,
        Deny = 2
    }
}
