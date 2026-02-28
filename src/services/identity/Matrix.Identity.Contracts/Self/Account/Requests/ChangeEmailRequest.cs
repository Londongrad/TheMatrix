namespace Matrix.Identity.Contracts.Self.Account.Requests
{
    public sealed class ChangeEmailRequest
    {
        public string NewEmail { get; init; } = null!;
        public string CurrentPassword { get; init; } = null!;
    }
}
