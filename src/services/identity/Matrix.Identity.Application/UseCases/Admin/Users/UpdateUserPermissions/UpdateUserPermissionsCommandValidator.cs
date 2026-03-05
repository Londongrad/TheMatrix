using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions
{
    public sealed class UpdateUserPermissionsCommandValidator : AbstractValidator<UpdateUserPermissionsCommand>
    {
        public UpdateUserPermissionsCommandValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty()
               .WithMessage("UserId must not be empty");

            RuleFor(x => x.Overrides)
               .NotNull()
               .WithMessage("Overrides must not be null");

            RuleForEach(x => x.Overrides)
               .ChildRules(overrideRule =>
                {
                    overrideRule.RuleFor(x => x.PermissionKey)
                       .NotEmpty()
                       .WithMessage("PermissionKey must not be empty");

                    overrideRule.RuleFor(x => x.Effect)
                       .Must(effect =>
                            string.Equals(effect, "Allow", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(effect, "Deny", StringComparison.OrdinalIgnoreCase))
                       .WithMessage("Effect must be Allow or Deny");
                });

            RuleFor(x => x.Overrides)
               .Must(HaveUniquePermissionKeys)
               .WithMessage("Permission keys must be unique");
        }

        private static bool HaveUniquePermissionKeys(
            IReadOnlyCollection<UpdateUserPermissionOverrideInput>? overrides)
        {
            if (overrides is null || overrides.Count <= 1)
                return true;

            int distinctCount = overrides
               .Where(x => !string.IsNullOrWhiteSpace(x.PermissionKey))
               .Select(x => x.PermissionKey.Trim())
               .Distinct(StringComparer.Ordinal)
               .Count();

            return distinctCount == overrides.Count;
        }
    }
}
