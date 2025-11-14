using MediatR;

namespace Matrix.Population.Application.UseCases.KillPerson
{
    public record class KillPersonCommand(Guid Id) : IRequest;
}
