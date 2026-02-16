namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class ForgotPasswordRequest
    {
        public required string Email { get; init; }
    }
}
