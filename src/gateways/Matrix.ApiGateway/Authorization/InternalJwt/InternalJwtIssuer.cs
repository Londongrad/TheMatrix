using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.ApiGateway.Authorization.InternalJwt
{
    public sealed class InternalJwtIssuer(IOptions<InternalJwtOptions> options) : IInternalJwtIssuer
    {
        private readonly InternalJwtOptions _options = options.Value;

        public string Issue(
            Guid userId,
            string? jti,
            int permissionsVersion,
            IReadOnlyCollection<string> permissions)
        {
            var claims = new List<Claim>
            {
                new(
                    type: JwtRegisteredClaimNames.Sub,
                    value: userId.ToString()),
                new(
                    type: JwtRegisteredClaimNames.Jti,
                    value: string.IsNullOrWhiteSpace(jti)
                        ? Guid.NewGuid()
                           .ToString()
                        : jti),
                new(
                    type: JwtClaimNames.PermissionsVersion,
                    value: permissionsVersion.ToString(CultureInfo.InvariantCulture))
            };

            // Детерминизм (удобно для дебага/сравнений)
            foreach (string permission in permissions
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(
                             keySelector: p => p,
                             comparer: StringComparer.Ordinal))
                claims.Add(
                    new Claim(
                        type: JwtClaimNames.Permission,
                        value: permission));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var credentials = new SigningCredentials(
                key: key,
                algorithm: SecurityAlgorithms.HmacSha256);

            DateTime expiresAtUtc = DateTime.UtcNow.AddSeconds(_options.LifetimeSeconds);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
