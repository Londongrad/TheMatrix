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
        private readonly InternalJwtOptions _options = options.Value;

        public string Issue(Guid subjectId)
        {
            var claims = new List<Claim>
            {
                new(
                    type: JwtRegisteredClaimNames.Sub,
                    value: subjectId.ToString()),
                new(
                    type: JwtRegisteredClaimNames.Jti,
                    value: Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var credentials = new SigningCredentials(
                key: key,
                algorithm: SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
