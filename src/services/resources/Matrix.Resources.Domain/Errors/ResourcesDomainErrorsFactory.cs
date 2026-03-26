using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.Resources.Domain.Errors
{
    public static class ResourcesDomainErrorsFactory
    {
        public static DomainException SimulationHostIdEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "Resources.SimulationHost.Id.Empty",
                message: "Simulation host id cannot be empty.",
                propertyName: propertyName);
        }
    }
}
