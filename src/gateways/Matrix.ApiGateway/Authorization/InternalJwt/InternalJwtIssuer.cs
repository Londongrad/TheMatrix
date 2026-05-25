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
    public sealed class InternalJwtIssuer(
        IOptions<InternalUserContextJwtOptions> options,
        TimeProvider timeProvider) : IInternalJwtIssuer
    {
        private readonly InternalJwtResolvedKeyRing _keyRing = ValidateOptions(options.Value);
        private readonly InternalUserContextJwtOptions _options = options.Value;
        private readonly TimeProvider _timeProvider = timeProvider;

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
                    type: JwtClaimNames.InternalTokenKind,
                    value: InternalJwtTokenKinds.UserContext),
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_keyRing.CurrentSigningKey));
            var credentials = new SigningCredentials(
                key: key,
                algorithm: SecurityAlgorithms.HmacSha256);

            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateTime issuedAtUtc = now.UtcDateTime;
            DateTime expiresAtUtc = now.AddSeconds(_options.LifetimeSeconds)
               .UtcDateTime;

            var header = new JwtHeader(credentials)
            {
                [JwtHeaderParameterNames.Kid] = _keyRing.CurrentKeyId
            };

            var payload = new JwtPayload(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: null,
                expires: expiresAtUtc,
                issuedAt: issuedAtUtc);

            var token = new JwtSecurityToken(
                header: header,
                payload: payload);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static InternalJwtResolvedKeyRing ValidateOptions(InternalUserContextJwtOptions options)
        {
            return InternalJwtKeyRingPolicy.Resolve(
                options: options,
                optionsPath: InternalUserContextJwtOptions.SectionName);
        }
    }
}
