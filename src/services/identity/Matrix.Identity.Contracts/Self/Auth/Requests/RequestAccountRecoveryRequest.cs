namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class RequestAccountRecoveryRequest
    {
        public string Email { get; init; } = null!;
    }
}
