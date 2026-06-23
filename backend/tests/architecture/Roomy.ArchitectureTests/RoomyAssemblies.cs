using System.Reflection;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

internal static class RoomyAssemblies
{
    internal static IReadOnlyCollection<Assembly> All { get; } = Discover();

    private static Assembly[] Discover()
    {
        var seen = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Enqueue(assembly);
        }

        Enqueue(ArchitectureConventions.SharedKernelAssembly);
        Enqueue(ArchitectureConventions.ApplicationContractsAssembly);
        Enqueue(ArchitectureConventions.InfrastructurePersistenceAssembly);
        Enqueue(ArchitectureConventions.InfrastructureMessagingAssembly);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var reference in current.GetReferencedAssemblies())
            {
                if (!IsRoomy(reference.Name) || seen.ContainsKey(reference.Name!)) continue;

                try
                {
                    Enqueue(Assembly.Load(reference));
                }
                catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
                {
                }
            }
        }

        return seen.Values
            .Where(assembly => assembly != Assembly.GetExecutingAssembly())
            .OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)
            .ToArray();

        void Enqueue(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            if (!IsRoomy(name) || !seen.TryAdd(name!, assembly)) return;

            queue.Enqueue(assembly);
        }
    }

    private static bool IsRoomy(string? simpleName) =>
        simpleName is not null
        && simpleName.StartsWith(ArchitectureConventions.RootNamespace, StringComparison.Ordinal);
}
