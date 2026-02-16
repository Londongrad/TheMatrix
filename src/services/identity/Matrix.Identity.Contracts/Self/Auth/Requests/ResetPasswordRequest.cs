namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class ResetPasswordRequest
    {
        public Guid UserId { get; init; }
        public required string Token { get; init; }
        public required string NewPassword { get; init; }
    }
}
