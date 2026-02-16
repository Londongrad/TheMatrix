namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class ConfirmEmailRequest
    {
        public Guid UserId { get; init; }
        public required string Token { get; init; }
    }
}
