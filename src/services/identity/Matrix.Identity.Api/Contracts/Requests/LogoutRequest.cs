namespace Matrix.Identity.Api.Contracts.Requests
{
    public sealed class LogoutRequest
    {
        public required string RefreshToken { get; set; }
    }
}
