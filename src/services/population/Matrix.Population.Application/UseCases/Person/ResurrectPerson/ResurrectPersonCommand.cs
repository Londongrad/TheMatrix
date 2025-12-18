using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.Person.ResurrectPerson
{
    public sealed record ResurrectPersonCommand(Guid Id) : IRequest<PersonDto>;
}
