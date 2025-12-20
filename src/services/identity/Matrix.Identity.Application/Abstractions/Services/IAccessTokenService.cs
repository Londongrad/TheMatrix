using Matrix.Identity.Application.UseCases.Auth;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IAccessTokenService
    {
        AccessTokenModel Generate(
            User user,
            IReadOnlyCollection<string> roles,
            IReadOnlyCollection<string> permissions,
            int permissionsVersion);
    }
}
