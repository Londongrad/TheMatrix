using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Simulation.Primitives.Tests;

public sealed class SimulationPrimitivesArchitectureTests
{
    [Fact]
    public void Assembly_ShouldNotDependOnOtherMatrixProjects()
    {
        string[] matrixDependencies = typeof(SimulationScenarioKey)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("Matrix.", StringComparison.Ordinal) == true)
            .Order()
            .ToArray()!;

        Assert.Empty(matrixDependencies);
    }
}
