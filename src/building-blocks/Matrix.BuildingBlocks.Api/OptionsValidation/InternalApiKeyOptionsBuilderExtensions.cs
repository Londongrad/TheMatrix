using Matrix.BuildingBlocks.Application.Security.InternalApiKey;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Matrix.BuildingBlocks.Api.OptionsValidation
{
    public static class InternalApiKeyOptionsBuilderExtensions
    {
        public static OptionsBuilder<TOptions> ValidateInternalApiKeyRing<TOptions>(
            this OptionsBuilder<TOptions> optionsBuilder,
            string optionsPath)
            where TOptions : class, IInternalApiKeyRingOptions
        {
            return optionsBuilder.Validate(
                validation: options => IsValidKeyRing(options, optionsPath),
                failureMessage: $"{optionsPath}: invalid key rotation configuration.");
        }

        private static bool IsValidKeyRing<TOptions>(
            TOptions options,
            string optionsPath)
            where TOptions : class, IInternalApiKeyRingOptions
        {
            try
            {
                _ = InternalApiKeyRingPolicy.Resolve(
                    options: options,
                    optionsPath: optionsPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
