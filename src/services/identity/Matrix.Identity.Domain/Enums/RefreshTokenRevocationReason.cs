namespace Matrix.Identity.Domain.Enums
{
    /// <summary>
    ///     Reason why a refresh token was revoked.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>
    ///                 <see cref="RefreshTokenRevocationReason.Unknown" />
    ///             </term>
    ///             <description>Fallback value when the revocation reason is not known or not provided.</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="RefreshTokenRevocationReason.PasswordChanged" />
    ///             </term>
    ///             <description>The user changed their password, so existing refresh tokens are no longer trusted.</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="RefreshTokenRevocationReason.UserLocked" />
    ///             </term>
    ///             <description>The user account was locked, so active sessions must be invalidated.</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="RefreshTokenRevocationReason.UserRevoked" />
    ///             </term>
    ///             <description>
    ///                 The user explicitly revoked the token (e.g., signed out from this device or ended the
    ///                 session).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="RefreshTokenRevocationReason.AdminRevoked" />
    ///             </term>
    ///             <description>An administrator explicitly revoked the token.</description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="RefreshTokenRevocationReason.SessionReplaced" />
    ///             </term>
    ///             <description>
    ///                 The session was replaced (e.g., a new refresh token was issued), so the previous one was
    ///                 revoked.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <see cref="RefreshTokenRevocationReason.SecurityEvent" />
    ///             </term>
    ///             <description>
    ///                 The token was revoked due to a security-related event (e.g., suspicious activity or policy
    ///                 enforcement).
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public enum RefreshTokenRevocationReason : byte
    {
        Unknown = 0,
        PasswordChanged = 1,
        UserLocked = 2,
        AdminRevoked = 3,
        UserRevoked = 4,
        SessionReplaced = 5,
        SecurityEvent = 6
    }
}
