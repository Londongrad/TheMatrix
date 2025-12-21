namespace Matrix.Identity.Contracts.Account.Requests
{
    public sealed class ChangePasswordRequest
    {
        public string CurrentPassword { get;} = null!;
        public string NewPassword { get;} = null!;
    }
}
