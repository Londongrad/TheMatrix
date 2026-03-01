namespace Matrix.Identity.Application.Abstractions.Services.Security
{
    public enum SecurityAuditEventType
    {
        Login = 1,
        EmailConfirmationRequested = 2,
        EmailConfirmed = 3,
        PasswordResetRequested = 4,
        PasswordResetCompleted = 5,
        Logout = 6,
        SessionRevoked = 7,
        AllSessionsRevoked = 8,
        UsernameChanged = 9,
        EmailChangeRequested = 10,
        EmailChanged = 11,
        AccountDeleted = 12,
        AccountRestored = 13
    }
}
