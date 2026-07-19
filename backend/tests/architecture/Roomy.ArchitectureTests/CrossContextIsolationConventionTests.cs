using NetArchTest.Rules;
using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

public sealed class CrossContextIsolationConventionTests
{
    private static readonly string[] contexts = ["Identity", "Organization", "Attendance"];

    [Fact]
    public void A_context_does_not_depend_on_another_contexts_types()
    {
        var inspectedContextTypes = 0;

        foreach (var assembly in RoomyAssemblies.All)
        {
            foreach (var context in contexts)
            {
                var contextNamespace = $"{ArchitectureConventions.RootNamespace}.{context}";
                var otherContextNamespaces = contexts
                    .Where(other => other != context)
                    .Select(other => $"{ArchitectureConventions.RootNamespace}.{other}")
                    .ToArray();

                var predicate = Types.InAssembly(assembly)
                    .That()
                    .ResideInNamespaceStartingWith(contextNamespace);

                inspectedContextTypes += predicate.GetTypes().Count();

                var result = predicate
                    .ShouldNot()
                    .HaveDependencyOnAny(otherContextNamespaces)
                    .GetResult();

                result.IsSuccessful.ShouldBeTrue(
                    SharedKernelPurityTests.FailureMessage($"context '{context}' must not reference another context's types", result)
                );
            }
        }

        inspectedContextTypes.ShouldBeGreaterThan(
            0,
            "Cross-context isolation rule inspected no types: 0 context types found across "
            + $"{RoomyAssemblies.All.Count} Roomy assembly(ies) — assembly discovery is broken "
            + "and the rule is passing vacuously.");
    }
}
