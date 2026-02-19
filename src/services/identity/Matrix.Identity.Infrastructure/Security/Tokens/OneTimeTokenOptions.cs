namespace Matrix.Identity.Infrastructure.Security.Tokens
{
    public sealed class OneTimeTokenOptions
    {
        public const string SectionName = "OneTimeTokens";

        public int EmailConfirmationLifetimeMinutes { get; init; } = 60 * 24;
        public int EmailConfirmationCooldownSeconds { get; init; } = 60;
        public int EmailConfirmationMaxDeliveryAttemptsPerHour { get; init; } = 5;

        public int PasswordResetLifetimeMinutes { get; init; } = 60;
        public int PasswordResetCooldownSeconds { get; init; } = 60;
        public int PasswordResetMaxDeliveryAttemptsPerHour { get; init; } = 5;
    }
}
