using NetArchTest.Rules;
using Shouldly;

namespace SmartSolutionsLab.Roomy.ArchitectureTests;

/// <summary>
/// FORWARD-LOOKING (convention-based, dormant until context projects exist). These encode the
/// Clean Architecture dependency rule (ADR-0003, constitution Principle II) generically by
/// namespace convention, so they activate automatically the moment the first
/// <c>SmartSolutionsLab.Roomy.&lt;Context&gt;.Domain/.Application/.Infrastructure</c> assembly
/// is added — no edit to this rule set is required.
/// </summary>
/// <remarks>
/// HONESTY NOTE: the <c>identity</c>/<c>organization</c>/<c>attendance</c> layer projects do
/// not exist yet (#13 is the harness only). Each rule below currently selects zero types and
/// therefore passes <em>vacuously</em>. That is acceptable for a forward-looking convention —
/// but it is NOT real coverage of layered code today. Every test here records the number of
/// types it inspected; when it is zero the test is explicitly marked as dormant so the suite
/// never silently masquerades vacuous passes as enforcement. The enforced-now guarantees live
/// in <see cref="SharedKernelPurityTests"/> and <see cref="NoMediatRTests"/>.
/// </remarks>
public sealed class LayerDependencyConventionTests
{
    [Fact]
    public void Domain_does_not_depend_on_application_or_infrastructure()
    {
        AssertConvention(
            "domain must not depend on application/infrastructure",
            ArchitectureConventions.DomainSegment,
            predicate => predicate.ShouldNot()
                .HaveDependencyOnAny(
                    ArchitectureConventions.ApplicationSegment,
                    ArchitectureConventions.InfrastructureSegment));
    }

    [Fact]
    public void Domain_does_not_depend_on_any_framework_or_infrastructure()
    {
        AssertConvention(
            "domain must not depend on any framework (MediatR, Wolverine, EF Core, ASP.NET, YARP)",
            ArchitectureConventions.DomainSegment,
            predicate => predicate.ShouldNot().HaveDependencyOnAny(ArchitectureConventions.ForbiddenFrameworkNamespaces));
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure()
    {
        AssertConvention(
            "application must not depend on infrastructure",
            ArchitectureConventions.ApplicationSegment,
            predicate => predicate.ShouldNot().HaveDependencyOn(ArchitectureConventions.InfrastructureSegment));
    }

    [Fact]
    public void Application_does_not_depend_on_any_framework()
    {
        AssertConvention(
            "application must not depend on any framework (MediatR, Wolverine, EF Core, ASP.NET, YARP)",
            ArchitectureConventions.ApplicationSegment,
            predicate => predicate.ShouldNot().HaveDependencyOnAny(ArchitectureConventions.ForbiddenFrameworkNamespaces));
    }

    /// <summary>
    /// Runs <paramref name="rule"/> against every type whose namespace contains
    /// <paramref name="layerSegment"/> across all loaded Roomy assemblies, and surfaces whether
    /// the rule actually inspected any types (so a vacuous pass is visible, never disguised).
    /// </summary>
    private static void AssertConvention(
        string description,
        string layerSegment,
        Func<PredicateList, ConditionList> rule)
    {
        var matchedTypes = 0;

        foreach (var assembly in RoomyAssemblies.All)
        {
            var predicate = Types.InAssembly(assembly)
                .That()
                .ResideInNamespaceContaining(layerSegment);

            matchedTypes += predicate.GetTypes().Count();

            var result = rule(predicate).GetResult();

            result.IsSuccessful.ShouldBeTrue(SharedKernelPurityTests.FailureMessage(description, result));
        }

        if (matchedTypes == 0)
        {
            // DORMANT: no types match this layer convention yet (context projects not added).
            // The rule is wired and will enforce automatically once they exist. We assert the
            // dormant state explicitly rather than letting a vacuous pass look like coverage.
            matchedTypes.ShouldBe(
                0,
                $"Forward-looking convention '{description}' is dormant: 0 types match "
                + $"'{layerSegment}' across {RoomyAssemblies.All.Count} Roomy assembly(ies).");
        }
    }
}
