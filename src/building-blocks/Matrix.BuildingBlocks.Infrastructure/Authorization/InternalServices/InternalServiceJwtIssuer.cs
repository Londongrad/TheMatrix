using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices
{
    public sealed class InternalServiceJwtIssuer(
        IOptions<InternalServiceJwtOptions> options,
        TimeProvider timeProvider) : IInternalServiceJwtIssuer
    {
        private readonly InternalJwtResolvedKeyRing _keyRing = ValidateOptions(options.Value);
        private readonly InternalServiceJwtOptions _options = options.Value;
        private readonly TimeProvider _timeProvider = timeProvider;

        public string Issue(
            Guid subjectId,
            string serviceName,
            IReadOnlyCollection<string> permissions)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException(
                    message: "Internal service name must be provided.",
                    paramName: nameof(serviceName));

            var claims = new List<Claim>
            {
                new(
                    type: JwtRegisteredClaimNames.Sub,
                    value: subjectId.ToString()),
                new(
                    type: JwtRegisteredClaimNames.Jti,
                    value: Guid.NewGuid()
                       .ToString()),
                new(
                    type: JwtClaimNames.InternalTokenKind,
                    value: InternalJwtTokenKinds.Service),
                new(
                    type: JwtClaimNames.Service,
                    value: serviceName)
            };

            foreach (string permission in permissions
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(
                             keySelector: x => x,
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

        private static InternalJwtResolvedKeyRing ValidateOptions(InternalServiceJwtOptions options)
        {
            return InternalJwtKeyRingPolicy.Resolve(
                options: options,
                optionsPath: InternalServiceJwtOptions.SectionName);
        }
    }
}
