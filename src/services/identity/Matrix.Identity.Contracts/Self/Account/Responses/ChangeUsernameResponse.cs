namespace Matrix.Identity.Contracts.Self.Account.Responses
{
    public sealed record ChangeUsernameResponse
    {
        public string Username { get; init; } = null!;
    }
}
