namespace Matrix.Identity.Infrastructure.Security.Tokens
{
    public sealed class OneTimeTokenOptions
    {
        public const string SectionName = "OneTimeTokens";

        public int EmailConfirmationLifetimeMinutes { get; init; } = 60 * 24;
        public int PasswordResetLifetimeMinutes { get; init; } = 60;
    }
}
