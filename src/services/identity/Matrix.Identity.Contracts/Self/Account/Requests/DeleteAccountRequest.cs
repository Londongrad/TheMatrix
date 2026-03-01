namespace Matrix.Identity.Contracts.Self.Account.Requests
{
    public sealed class DeleteAccountRequest
    {
        public string CurrentPassword { get; init; } = null!;
    }
}
