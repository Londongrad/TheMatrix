using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.UseCases;
using Matrix.Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Matrix.Identity.Infrastructure.Authentication.Jwt
{
    public sealed class JwtAccessTokenService(IOptions<JwtOptions> options) : IAccessTokenService
    {
        private readonly JwtOptions _options = options.Value;

        public AccessTokenModel Generate(User user)
        {
            var username = user.Username.Value;

            var claims = new List<Claim>
            {
                // основной идентификатор пользователя
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

                // email
                new(JwtRegisteredClaimNames.Email, user.Email.Value),

                // username как "unique_name" + дублируем в ClaimTypes.Name
                new(JwtRegisteredClaimNames.UniqueName, username),
                new(ClaimTypes.Name, username),
                new("username", username),

                // id токена
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AccessTokenModel
            {
                Token = tokenString,
                ExpiresInSeconds = _options.AccessTokenLifetimeMinutes * 60
            };
        }
    }
}
