namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class ConfirmAccountRecoveryRequest
    {
        public Guid UserId { get; init; }
        public string Token { get; init; } = null!;
    }
}
