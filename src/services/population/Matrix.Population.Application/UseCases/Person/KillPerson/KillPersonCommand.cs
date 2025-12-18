using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.KillPerson
{
    public record class KillPersonCommand(Guid Id) : IRequest<PersonDto>;
}
