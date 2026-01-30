using Matrix.ApiGateway.Services.CityCore.Cities;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class GatewayCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddGatewayCore(this IServiceCollection services)
        {
            services
               .AddScoped<ICityProvisioningService, CityProvisioningService>()
               .AddGatewayControllers()
               .AddGatewayCors();

            return services;
        }

        private static IServiceCollection AddGatewayControllers(this IServiceCollection services)
        {
            services.AddControllers()
               .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                           .Where(kvp => kvp.Value?.Errors.Count > 0)
                           .ToDictionary(
                                keySelector: kvp => kvp.Key,
                                elementSelector: kvp => kvp.Value!.Errors
                                   .Select(e => e.ErrorMessage)
                                   .ToArray());

                        var error = new ErrorResponse(
                            Code: "Gateway.ValidationError",
                            Message: "Validation failed.",
                            Errors: errors,
                            TraceId: context.HttpContext.TraceIdentifier);

                        return new BadRequestObjectResult(error);
                    };
                });

            return services;
        }

        private static IServiceCollection AddGatewayCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: GatewayCorsDefaults.PolicyName,
                    configurePolicy: policy =>
                    {
                        policy
                           .WithOrigins("https://localhost:5173")
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                    });
            });

            return services;
        }
    }
}
