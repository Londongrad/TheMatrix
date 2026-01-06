using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.UseCases.Self.Auth;
using Matrix.Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.Identity.Infrastructure.Authentication.Jwt
{
    public sealed class JwtAccessTokenService(IOptions<JwtOptions> options) : IAccessTokenService
    {
        private readonly JwtOptions _options = options.Value;

        public AccessTokenModel Generate(
            User user,
            IReadOnlyCollection<string> roles,
            IReadOnlyCollection<string> permissions,
            int permissionsVersion)
        {
            string username = user.Username.Value;

            var claims = new List<Claim>
            {
                new(
                    type: JwtRegisteredClaimNames.Sub,
                    value: user.Id.ToString()),
                new(
                    type: JwtRegisteredClaimNames.Email,
                    value: user.Email.Value),
                new(
                    type: JwtRegisteredClaimNames.UniqueName,
                    value: username),
                new(
                    type: ClaimTypes.Name,
                    value: username),
                new(
                    type: "username",
                    value: username),
                new(
                    type: JwtRegisteredClaimNames.Jti,
                    value: Guid.NewGuid()
                       .ToString()),
                new(
                    type: JwtClaimNames.PermissionsVersion,
                    value: permissionsVersion.ToString(CultureInfo.InvariantCulture))
            };

            foreach (string role in roles.OrderBy(
                         keySelector: x => x,
                         comparer: StringComparer.Ordinal))
                claims.Add(
                    new Claim(
                        type: ClaimTypes.Role,
                        value: role));

            foreach (string permission in permissions.OrderBy(
                         keySelector: x => x,
                         comparer: StringComparer.Ordinal))
                claims.Add(
                    new Claim(
                        type: PermissionClaimTypes.Permission,
                        value: permission));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var credentials = new SigningCredentials(
                key: key,
                algorithm: SecurityAlgorithms.HmacSha256);

            DateTime expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            string? tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AccessTokenModel
            {
                Token = tokenString,
                ExpiresInSeconds = _options.AccessTokenLifetimeMinutes * 60
            };
        }
    }
}
