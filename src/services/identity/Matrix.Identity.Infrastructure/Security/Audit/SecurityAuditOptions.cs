namespace Matrix.Identity.Infrastructure.Security.Audit
{
    public sealed class SecurityAuditOptions
    {
        public const string SectionName = "SecurityAudit";

        public int FailedLoginWindowMinutes { get; init; } = 15;
        public int FailedLoginMaxAttemptsPerLogin { get; init; } = 10;
        public int FailedLoginMaxAttemptsPerIp { get; init; } = 20;

        public int EmailConfirmationRequestWindowMinutes { get; init; } = 60;
        public int EmailConfirmationRequestMaxAttemptsPerEmail { get; init; } = 5;
        public int EmailConfirmationRequestMaxAttemptsPerIp { get; init; } = 20;

        public int EmailChangeRequestWindowMinutes { get; init; } = 60;
        public int EmailChangeRequestMaxAttemptsPerEmail { get; init; } = 5;
        public int EmailChangeRequestMaxAttemptsPerIp { get; init; } = 20;

        public int PasswordResetRequestWindowMinutes { get; init; } = 60;
        public int PasswordResetRequestMaxAttemptsPerEmail { get; init; } = 5;
        public int PasswordResetRequestMaxAttemptsPerIp { get; init; } = 20;
    }
}
