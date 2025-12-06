using System.ComponentModel.DataAnnotations;

namespace Matrix.Identity.Api.Contracts.Requests
{
    public sealed class RegisterRequest
    {
        [Required] [EmailAddress] public string Email { get; set; } = null!;

        [Required]
        [MinLength(3)]
        [MaxLength(32)]
        public string Username { get; set; } = null!;

        [Required] [MinLength(6)] public string Password { get; set; } = null!;

        [Required] [Compare(nameof(Password))] public string ConfirmPassword { get; set; } = null!;
    }
}
