using Matrix.BuildingBlocks.Application.Behaviors;
using Matrix.BuildingBlocks.Application.DependencyInjection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Matrix.BuildingBlocks.Application.Tests.DependencyInjection
{
    public sealed class ApplicationPipelineServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDefaultApplicationPipeline_WhenCalled_RegistersDefaultBehaviors()
        {
            ServiceCollection services = new();

            services.AddDefaultApplicationPipeline();

            Type[] implementations = services
               .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
               .Select(descriptor => descriptor.ImplementationType)
               .OfType<Type>()
               .ToArray();

            Assert.Equal(
                expected:
                [
                    typeof(LoggingBehavior<,>),
                    typeof(PermissionBehavior<,>),
                    typeof(ValidationBehavior<,>)
                ],
                actual: implementations);
        }
    }
}
