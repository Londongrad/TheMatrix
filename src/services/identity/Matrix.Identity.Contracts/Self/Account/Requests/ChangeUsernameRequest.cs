namespace Matrix.Identity.Contracts.Self.Account.Requests
{
    public sealed class ChangeUsernameRequest
    {
        public string Username { get; init; } = null!;
        public string CurrentPassword { get; init; } = null!;
    }
}
