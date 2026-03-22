using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.SimulationSystems.Domain.Errors
{
    public static class SimulationSystemsDomainErrorsFactory
    {
        public static DomainException SimulationHostIdEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.SimulationHost.Id.Empty",
                message: "Simulation host id cannot be empty.",
                propertyName: propertyName);
        }
    }
}
