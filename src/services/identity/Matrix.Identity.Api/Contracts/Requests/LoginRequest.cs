using System.ComponentModel.DataAnnotations;

namespace Matrix.Identity.Api.Contracts.Requests
{
    public sealed class LoginRequest
    {
        [Required] public string Login { get; set; } = null!;

        [Required] public string Password { get; set; } = null!;

        [Required] public string DeviceId { get; set; } = null!;

        [Required] public string DeviceName { get; set; } = null!;
    }
}
