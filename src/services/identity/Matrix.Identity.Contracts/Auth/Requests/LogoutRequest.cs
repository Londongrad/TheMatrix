namespace Matrix.Identity.Contracts.Auth.Requests
{
    public sealed class LogoutRequest
    {
        public required string RefreshToken { get; init; }
    }
}
