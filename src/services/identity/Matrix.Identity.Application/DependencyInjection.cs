using Matrix.Identity.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Identity.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(IUserRepository).Assembly);
            });
        }
    }
}
