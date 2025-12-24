namespace Matrix.Identity.Contracts.Self.Account.Requests
{
    public sealed class ChangePasswordRequest
    {
        public string CurrentPassword { get; } = null!;
        public string NewPassword { get; } = null!;
    }
}
