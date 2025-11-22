using System.ComponentModel.DataAnnotations;

namespace Matrix.Identity.Api.Contracts
{
    public sealed class LoginRequest
    {
        [Required]
        public string Login { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}