using FluentValidation.Results;
using Matrix.Population.Application.UseCases.Person.ResurrectPerson;
using Xunit;

namespace Matrix.Population.Application.Tests.UseCases.Person.ResurrectPerson
{
    public sealed class ResurrectPersonCommandValidatorTests
    {
        private readonly ResurrectPersonCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new ResurrectPersonCommand(Guid.Parse("11111111-2222-3333-4444-555555555555")));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new ResurrectPersonCommand(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Id");
        }
    }
}
