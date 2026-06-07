using NetArchTest.Rules;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// FORWARD-LOOKING (convention-based, dormant until context projects exist). Encodes context
/// isolation (ADR-0003 / constitution Principle III): a bounded context must not reference
/// another context's types — cross-context communication is by ID and integration events only.
/// </summary>
/// <remarks>
/// HONESTY NOTE: the three contexts (<c>identity</c>, <c>organization</c>, <c>attendance</c>)
/// do not exist as assemblies yet, so this rule currently inspects zero context types and is
/// dormant. It activates automatically once the context assemblies are added — for each known
/// context it forbids any dependency on the others' namespaces.
/// </remarks>
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

                Assert.True(
                    result.IsSuccessful,
                    SharedKernelPurityTests.FailureMessage(
                        $"context '{context}' must not reference another context's types",
                        result));
            }
        }

        if (inspectedContextTypes == 0)
        {
            // DORMANT: no context assemblies present yet; rule is wired for when they arrive.
            Assert.True(
                true,
                "Forward-looking cross-context isolation rule is dormant: 0 context types found "
                + $"across {RoomyAssemblies.All.Count} Roomy assembly(ies).");
        }
    }
}
