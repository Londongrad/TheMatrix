using Matrix.Healthcare.Application.Care;

namespace Matrix.Healthcare.Application.Abstractions;

public interface ICareDeliveryActivityOutboxWriter
{
    Task AddAsync(
        CareDeliveryActivitySnapshot activity,
        CancellationToken cancellationToken = default);
}
