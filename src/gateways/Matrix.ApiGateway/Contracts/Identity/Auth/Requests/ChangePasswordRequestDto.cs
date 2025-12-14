using System.ComponentModel.DataAnnotations;

namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public sealed class ChangePasswordRequestDto
    {
        [Required]
        [StringLength(
            20,
            MinimumLength = 6)]
        public string CurrentPassword { get; set; } = null!;

        [Required]
        [StringLength(
            20,
            MinimumLength = 6)]
        public string NewPassword { get; set; } = null!;

        [Required]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "New password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
