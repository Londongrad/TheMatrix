using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Infrastructure.Security.Tokens
{
    public class OneTimeTokenService : IOneTimeTokenService
    {
        public string GenerateRawToken()
        {
            throw new NotImplementedException();
        }

        public string HashToken(string rawToken)
        {
            throw new NotImplementedException();
        }

        public TimeSpan GetTtl(OneTimeTokenPurpose purpose)
        {
            throw new NotImplementedException();
        }
    }
}
