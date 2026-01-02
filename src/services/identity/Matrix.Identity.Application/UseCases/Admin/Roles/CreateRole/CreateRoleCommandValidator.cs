using FluentValidation;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole
{
    public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty()
               .WithMessage("Name must not be empty")
               .MaximumLength(Role.NameMaxLength)
               .WithMessage($"Role name must be at most {Role.NameMaxLength} characters");
        }
    }
}
