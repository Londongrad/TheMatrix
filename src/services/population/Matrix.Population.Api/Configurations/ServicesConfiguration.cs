using System.Text;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application;
using Matrix.Population.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.Population.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.Services.AddControllers();

            // JWT auth
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
                {
                    IConfigurationSection jwt = configuration.GetSection("InternalJwt");
                    string issuer = jwt["Issuer"]!;
                    string audience = jwt["Audience"]!;
                    string signingKey = jwt["SigningKey"]!;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
