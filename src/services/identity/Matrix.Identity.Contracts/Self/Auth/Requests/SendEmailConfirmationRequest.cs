namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class SendEmailConfirmationRequest
    {
        public required string Email { get; init; }
    }
}
