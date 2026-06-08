using System.Reflection;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// Discovers the loaded Roomy code assemblies so the convention-based rules apply to every
/// context assembly automatically as it is added — no edit to the rule set required.
/// </summary>
/// <remarks>
/// The discovery is anchored on the shared-kernel assembly (guaranteed loaded, since this
/// project references it) and walks its transitive references, loading any whose simple
/// name starts with the Roomy root namespace. As the <c>identity</c>/<c>organization</c>/
/// <c>attendance</c> domain, application, and infrastructure projects come online and are
/// referenced into the relevant hosts, they enter this set without code changes here.
/// </remarks>
internal static class RoomyAssemblies
{
    /// <summary>All loaded Roomy code assemblies, excluding the architecture-test assembly itself.</summary>
    internal static IReadOnlyCollection<Assembly> All { get; } = Discover();

    private static Assembly[] Discover()
    {
        var seen = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>();

        // Seed with already-loaded Roomy assemblies plus the known shared-kernel anchor.
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Enqueue(assembly);
        }

        Enqueue(ArchitectureConventions.SharedKernelAssembly);
        Enqueue(ArchitectureConventions.ApplicationContractsAssembly);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var reference in current.GetReferencedAssemblies())
            {
                if (!IsRoomy(reference.Name) || seen.ContainsKey(reference.Name!))
                {
                    continue;
                }

                try
                {
                    Enqueue(Assembly.Load(reference));
                }
                catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
                {
                    // A referenced Roomy assembly that cannot be loaded in the test host is
                    // simply skipped; it will be inspected once it is genuinely on the path.
                }
            }
        }

        return seen.Values
            .Where(a => a != Assembly.GetExecutingAssembly())
            .OrderBy(a => a.GetName().Name, StringComparer.Ordinal)
            .ToArray();

        void Enqueue(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            if (!IsRoomy(name) || !seen.TryAdd(name!, assembly))
            {
                return;
            }

            queue.Enqueue(assembly);
        }
    }

    private static bool IsRoomy(string? simpleName) =>
        simpleName is not null
        && simpleName.StartsWith(ArchitectureConventions.RootNamespace, StringComparison.Ordinal);
}
