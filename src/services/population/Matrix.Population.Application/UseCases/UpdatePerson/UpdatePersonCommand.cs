using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.UpdatePerson
{
    public sealed record UpdatePersonCommand(
        Guid Id,
        UpdatePersonRequest Changes
    ) : IRequest<PersonDto>;
}
