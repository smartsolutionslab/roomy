using System.Reflection;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

internal static class RoomyAssemblies
{
    internal static IReadOnlyCollection<Assembly> All { get; } = Discover();

    private static Assembly[] Discover() =>
        Directory
            .EnumerateFiles(AppContext.BaseDirectory, $"{ArchitectureConventions.RootNamespace}.*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(assemblyName => assemblyName is not null)
            .OrderBy(assemblyName => assemblyName, StringComparer.Ordinal)
            .Select(assemblyName => Load(assemblyName!))
            .Where(assembly => assembly != Assembly.GetExecutingAssembly())
            .ToArray();

    private static Assembly Load(string assemblyName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Roomy assembly '{assemblyName}' is present in the test output directory but "
                + "could not be loaded; its architecture rules would silently not run.",
                exception);
        }
    }
}
