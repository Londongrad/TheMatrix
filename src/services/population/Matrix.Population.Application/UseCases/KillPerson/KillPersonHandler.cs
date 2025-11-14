using Matrix.Population.Application.Abstractions;
using MediatR;

namespace Matrix.Population.Application.UseCases.KillPerson
{
    public class KillPersonHandler(
        IPersonReadRepository personReadRepository,
        IPersonWriteRepository personWriteRepository) 
        : IRequestHandler<KillPersonCommand>
    {
        public async Task Handle(KillPersonCommand request, CancellationToken cancellationToken = default)
        {
            var person = await personReadRepository.GetByIdAsync(request.Id, cancellationToken) 
                ?? throw new KeyNotFoundException($"Person with Id {request.Id} not found.");

            person.Die();

            await personWriteRepository.UpdateAsync(person, cancellationToken); 
        }
    }
}
