using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.UseCases.Auth;
using Matrix.Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.Identity.Infrastructure.Authentication.Jwt
{
    public sealed class JwtAccessTokenService(IOptions<JwtOptions> options) : IAccessTokenService
    {
        private readonly JwtOptions _options = options.Value;

        public AccessTokenModel Generate(User user)
        {
            string username = user.Username.Value;

            var claims = new List<Claim>
            {
                // основной идентификатор пользователя
                new(
                    type: JwtRegisteredClaimNames.Sub,
                    value: user.Id.ToString()),

                // email
                new(
                    type: JwtRegisteredClaimNames.Email,
                    value: user.Email.Value),

                // username как "unique_name" + дублируем в ClaimTypes.Name
                new(
                    type: JwtRegisteredClaimNames.UniqueName,
                    value: username),
                new(
                    type: ClaimTypes.Name,
                    value: username),
                new(
                    type: "username",
                    value: username),

                // id токена
                new(
                    type: JwtRegisteredClaimNames.Jti,
                    value: Guid.NewGuid()
                               .ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var creds = new SigningCredentials(
                key: key,
                algorithm: SecurityAlgorithms.HmacSha256);

            DateTime expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            string? tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AccessTokenModel
            {
                Token = tokenString,
                ExpiresInSeconds = _options.AccessTokenLifetimeMinutes * 60
            };
        }
    }
}
