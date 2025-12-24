namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class LogoutRequest
    {
        public required string RefreshToken { get; init; }
    }
}
