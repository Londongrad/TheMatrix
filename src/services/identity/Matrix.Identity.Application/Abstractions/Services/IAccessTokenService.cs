using Matrix.Identity.Application.UseCases.Auth;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IAccessTokenService
    {
        AccessTokenModel Generate(User user);
    }
}
