namespace Matrix.Identity.Api.Contracts
{
    public sealed class RefreshRequest
    {
        public required string RefreshToken { get; set; }
    }
}