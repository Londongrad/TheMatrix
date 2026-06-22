using System.Reflection;

namespace Matrix.ArchitectureTesting
{
    public static class BoundedContextDependencyRule
    {
        public static void AssertOnlyReferencesMatrixAssemblies(
            Assembly assembly,
            params string[] allowedAssemblyNames)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(allowedAssemblyNames);

            HashSet<string> allowed = allowedAssemblyNames.ToHashSet(StringComparer.Ordinal);
            string[] forbiddenReferences = assembly.GetReferencedAssemblies()
               .Select(reference => reference.Name)
               .Where(name => name is not null && name.StartsWith("Matrix.", StringComparison.Ordinal))
               .Cast<string>()
               .Where(name => !allowed.Contains(name))
               .Order(StringComparer.Ordinal)
               .ToArray();

            if (forbiddenReferences.Length > 0)
                throw new InvalidOperationException(
                    $"Assembly '{assembly.GetName().Name}' has forbidden Matrix dependencies: " +
                    string.Join(", ", forbiddenReferences));
        }
    }
}
