using Matrix.Identity.Application.UseCases.Self.Auth;

namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IAccessTokenService
    {
        AccessTokenModel Generate(
            Guid userId,
            int permissionsVersion);
    }
}
