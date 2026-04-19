using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices
{
    public sealed class InternalServiceJwtIssuer(IOptions<InternalJwtOptions> options) : IInternalServiceJwtIssuer
    {
        private readonly InternalJwtOptions _options = ValidateOptions(options.Value);

        public string Issue(
            Guid subjectId,
            string serviceName,
            IReadOnlyCollection<string> permissions)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Internal service name must be provided.", nameof(serviceName));

            var claims = new List<Claim>
            {
                new(
                    type: JwtRegisteredClaimNames.Sub,
                    value: subjectId.ToString()),
                new(
                    type: JwtRegisteredClaimNames.Jti,
                    value: Guid.NewGuid().ToString()),
                new(
                    type: JwtClaimNames.Service,
                    value: serviceName)
            };

            foreach (string permission in permissions
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(x => x, StringComparer.Ordinal))
            {
                claims.Add(new Claim(
                    type: JwtClaimNames.Permission,
                    value: permission));
            }

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

        private static InternalJwtOptions ValidateOptions(InternalJwtOptions options)
        {
            InternalJwtSigningKeyPolicy.EnsureStrong(
                signingKey: options.SigningKey,
                optionsPath: InternalJwtOptions.SectionName);

            return options;
        }
    }
}
