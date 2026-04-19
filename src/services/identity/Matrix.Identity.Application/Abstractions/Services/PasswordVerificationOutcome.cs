namespace Matrix.Identity.Application.Abstractions.Services
{
    public readonly record struct PasswordVerificationOutcome(
        bool Succeeded,
        bool RequiresRehash)
    {
        public static PasswordVerificationOutcome Failed => new(
            Succeeded: false,
            RequiresRehash: false);

        public static PasswordVerificationOutcome Success => new(
            Succeeded: true,
            RequiresRehash: false);

        public static PasswordVerificationOutcome SuccessRehashNeeded => new(
            Succeeded: true,
            RequiresRehash: true);
    }
}
